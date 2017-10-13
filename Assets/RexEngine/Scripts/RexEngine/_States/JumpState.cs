/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class JumpState:RexState
	{
		public enum JumpType
		{
			Finite,
			Infinite,
			None
		}

		[System.Serializable]
		public class Animations
		{
			public AnimationClip startingJump;
			public AnimationClip crestingJump;
		}

		public enum Substate
		{
			Starting,
			Body,
			Cresting
		}

		public float speed = 12.0f;
		public JumpType type;
		public int multipleJumpNumber = 1;
		public bool canMultiJumpOutOfFall = true;
		public int minFrames = 12;
		public int maxFrames = 15;
		public bool freezeHorizontalMovement; //Setting this to True prevents you from maneuvering in midair after a jump is started

		public const string idString = "Jumping";

		public Animations animations;

		[HideInInspector]
		public Substate substate;

		[HideInInspector]
		public Direction.Horizontal direction;

		[HideInInspector]
		public bool isGroundedWithJumpButtonUp = false;

		[HideInInspector]
		public int framesFrozenForWallJump;

		protected bool hasReleasedButtonSinceJump;
		protected int currentJumpFrame = 0;
		protected float jumpStartY; //Just used for logging the jump height
		protected int currentJump;
		protected DashState dashState;
		protected bool isJumpActive = false;
		protected bool isWallJumpKickbackActive;
		protected WallClingState wallClingState;

		void Awake()
		{
			id = idString;
			isConcurrent = true;
			willPlayAnimationOnBegin = false;
			dashState = GetComponent<DashState>();
			wallClingState = GetComponent<WallClingState>();
			GetController();
		}

		void Update() 
		{
			if(!IsFrozen() && controller.isEnabled)
			{
				bool isJumpAttempted = controller.slots.input && controller.slots.input.isJumpButtonDownThisFrame;
				willAllowDirectionChange = (isJumpActive && freezeHorizontalMovement) ? false : true;
				if(isJumpAttempted)
				{
					Begin(true);
				}

				if(isJumpActive && controller.slots.input && !controller.slots.input.isJumpButtonDown)
				{
					hasReleasedButtonSinceJump = true;
				}
			}
		}

		void FixedUpdate()
		{
			if(Time.timeScale > 0)
			{
				ResetFlags();
				framesFrozenForWallJump --;
				if(framesFrozenForWallJump < 0)
				{
					framesFrozenForWallJump = 0;
					isWallJumpKickbackActive = false;
				}
			}
		}

		#region unique public methods

		public void OnBounce()
		{
			currentJump = 1;
		}

		public void OnLadderExit()
		{
			currentJump = 0;
		}

		public bool CanEnd()
		{
			return (currentJumpFrame > 1) ? true : false;
		}

		public bool IsHorizontalMovementFrozen()
		{
			return freezeHorizontalMovement || isWallJumpKickbackActive;
		}

		public void NotifyOfWallJump(int framesToFreeze, Direction.Horizontal kickbackDirection)
		{
			direction = kickbackDirection;
			framesFrozenForWallJump = framesToFreeze;
			ForceBegin();
			currentJump = 0;
			isWallJumpKickbackActive = true;
		}

		#endregion

		#region override public methods

		public override void OnNewStateAdded(RexState _state)
		{
			if(_state.id == WallClingState.idString && wallClingState == null)
			{
				wallClingState = _state as WallClingState;
			}	
		}

		public override bool CanInitiate()
		{
			bool canInitiateJump = false;
			if(type == JumpType.None)
			{
				canInitiateJump = false;
			}
			else if(controller.StateID() == CrouchState.idString && (!controller.GetComponent<CrouchState>().CanExitCrouch() || !controller.GetComponent<CrouchState>().canJump))
			{
				canInitiateJump = false;
			}
			else if(controller.slots.input && controller.slots.input.verticalAxis == -1.0f && RaycastHelper.DropThroughFloorRaycast((Direction.Vertical)(controller.GravityScaleMultiplier() * -1.0f), controller.slots.actor.GetComponent<BoxCollider2D>())) //Dropping through a one-way ledge instead of jumping
			{
				canInitiateJump = false;
			}
			else if(IsLockedForAttack(Attack.ActionType.Jumping))
			{
				canInitiateJump = false;
			}
			else if(controller.isKnockbackActive || controller.isStunned)
			{
				canInitiateJump = false;
			}
			else if(controller.isDashing && dashState && !dashState.canJump)
			{
				canInitiateJump = false;
			}
			else if(controller.slots.actor && controller.StateID() == LadderState.idString)
			{
				canInitiateJump = false;
			}
			else if(controller.framesSinceDrop < 2)
			{
				canInitiateJump = false;
			}
			else if(wallClingState && wallClingState.IsWallJumpPossible())
			{
				canInitiateJump = false;
			}
			else
			{
				if(type == JumpType.Finite)
				{
					WallClingState wallClingState = controller.GetComponent<WallClingState>();
					if(multipleJumpNumber > 0)
					{
						if((currentJump == 0 && (isGroundedWithJumpButtonUp || (!controller.slots.input && controller.slots.physicsObject.IsOnSurface()))) || (currentJump < multipleJumpNumber && currentJump > 0))
						{
							currentJump ++;
							canInitiateJump = true;
						}
						else if(multipleJumpNumber > 1 && !isJumpActive && canMultiJumpOutOfFall && currentJump < multipleJumpNumber)
						{
							currentJump += 2;
							canInitiateJump = true;
						}
					}
				}
				else if(type == JumpType.Infinite)
				{
					currentJump ++;
					canInitiateJump = true;
				}
			}

			return canInitiateJump;
		}

		public override void OnBegin()
		{
			currentJumpFrame = 0;
			isJumpActive = true;
			hasReleasedButtonSinceJump = false;
			controller.slots.physicsObject.properties.isFalling = false;
			isGroundedWithJumpButtonUp = false;

			if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.canceledBy.onJump)
			{
				controller.slots.actor.currentAttack.Cancel();
			}

			if(freezeHorizontalMovement)
			{
				direction = (Direction.Horizontal)controller.slots.input.horizontalAxis;
			}

			/*float velocity = speed;
			controller.slots.physicsObject.AddVelocityForSingleFrame(new Vector2(0.0f, velocity * controller.GravityScaleMultiplier()));*/

			StartCoroutine("JumpCoroutine");

			controller.slots.actor.NotifyOfControllerJumping(currentJump);
		}

		public override void OnEnded()
		{
			currentJumpFrame = 0;
			isJumpActive = false;
			framesFrozenForWallJump = 0;
			StopCoroutine("JumpCoroutine");
		}

		public override void OnStateChanged()
		{
			if(controller.StateID() == DashState.idString)
			{
				currentJumpFrame = 0;
				framesFrozenForWallJump = 0;
			}
			else if(controller.StateID() == LadderState.idString)
			{
				End();
			}
			else if(controller.StateID() == KnockbackState.idString)
			{
				End();
			}
		}

		public override void UpdateMovement()
		{
			if(!controller.isKnockbackActive)
			{
				if((controller.slots.input != null && controller.slots.input.isJumpButtonDown && !hasReleasedButtonSinceJump) || (currentJumpFrame < minFrames && isJumpActive))
				{
					if(currentJumpFrame == 0)
					{
						jumpStartY = transform.position.y; //Just used for debugging
					}

					if(isJumpActive) //Continue a jump
					{
						currentJumpFrame ++;

						int totalJumpFrames = maxFrames;
						if(currentJumpFrame >= totalJumpFrames || controller.slots.physicsObject.DidHitCeilingThisFrame()) //The jump ends because we hit the max number of frames we allowed it
						{
							substate = Substate.Cresting;
							isJumpActive = false;

							if(!controller.slots.actor.IsAttacking() && controller.StateID() == id)
							{
								PlaySecondaryAnimation(animations.crestingJump);
							}

							controller.slots.actor.NotifyOfControllerJumpCresting();

							float jumpHeight = Mathf.Abs(transform.position.y - jumpStartY);
							//Debug.Log("Position is: " + transform.position.y + "     Jump height: " + jumpHeight);
						}
						else
						{
							controller.slots.physicsObject.AddVelocityForSingleFrame(new Vector2(0.0f, speed * controller.GravityScaleMultiplier()));
						}
					}
				}
				else //The jump ends because the input requesting the jump ended
				{
					if(isJumpActive)
					{
						substate = Substate.Cresting;
						isJumpActive = false;

						if(!controller.slots.actor.IsAttacking() && controller.StateID() == id)
						{
							PlaySecondaryAnimation(animations.crestingJump);
						}

						controller.slots.actor.NotifyOfControllerJumpCresting();
					}
				}
			}
			else
			{
				currentJumpFrame = 0;
				isJumpActive = false;
			}
		}

		public override void PlayAnimationForSubstate()
		{
			AnimationClip animationToPlay = animation;
			switch(substate)
			{
				case Substate.Body:
					animationToPlay = animation;
					break;
				case Substate.Cresting:
					animationToPlay = animations.crestingJump;
					break;
				case Substate.Starting:
					animationToPlay = animations.startingJump;
					break;
			}

			if(controller.StateID() == id)
			{
				PlaySecondaryAnimation(animationToPlay);
			}
		}

		public bool IsJumpActive()
		{
			return isJumpActive;
		}

		#endregion

		protected virtual IEnumerator JumpCoroutine()
		{
			substate = Substate.Starting;
			float duration = 0.0f;

			if(!controller.slots.actor.IsAttacking())
			{
				PlaySecondaryAnimation(animations.startingJump);
			}

			yield return new WaitForSeconds(duration);

			if(isJumpActive)
			{
				substate = Substate.Body;

				if(!controller.slots.actor.IsAttacking())
				{
					PlayAnimation();
				}
			}
		}

		protected void ResetFlags()
		{
			if(controller.slots.physicsObject.IsOnSurface() && controller.slots.input && !controller.slots.input.isJumpButtonDown && !isJumpActive)
			{
				isGroundedWithJumpButtonUp = true;
			}
			else if(!controller.slots.physicsObject.IsOnSurface() || isJumpActive)
			{
				isGroundedWithJumpButtonUp = false;
			}

			if(controller.slots.physicsObject.IsOnSurface() && !isJumpActive)
			{
				currentJump = 0;
			}
		}
	}

}
