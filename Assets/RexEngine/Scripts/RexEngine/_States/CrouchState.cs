/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class CrouchState:RexState 
	{
		public const string idString = "Crouching";

		public Vector2 colliderSize = new Vector2(1.0f, 0.9f);
		public Vector2 colliderOffset = new Vector2(0.0f, -0.48f);
		public float moveSpeed = 5.0f;
		public bool willRiseWithButtonRelease;
		public bool canMove;
		public bool canJump;
		public bool mustReleaseButtonToMove = true;
		public bool immediatelyKillDecelerationOnCrouch = true;
		public AnimationClip movingAnimation;
		public bool allowAccelerationOnMove;

		[HideInInspector]
		public bool isSkidComplete;

		[HideInInspector]
		public bool hasPlayerReleasedHorizontal = false;

		protected bool hasStoppedMovingSinceCrouch;

		protected BoxCollider2D boxCollider;

		protected Substate substate;
		protected Vector2 nonCrouchingColliderSize;
		protected Vector2 nonCrouchingColliderOffset;

		public enum Substate
		{
			Stopped,
			Moving
		}

		void Awake() 
		{
			id = idString;
			GetController();
		}

		void Start()
		{
			GetCollider();

			if(!controller)
			{
				controller = GetComponent<RexController>();
				if(controller)
				{
					controller.AddState(this);
				}
			}

			EnemyAI enemyAI = GetComponent<EnemyAI>();
			if(enemyAI)
			{
				enemyAI.OnNewStateAdded(this);
			}
		}

		void Update()
		{
			if(!IsFrozen() && isEnabled && controller.isEnabled)
			{
				if(controller.axis.y == -1.0f)
				{
					if(controller.slots.physicsObject.IsOnSurface() && controller.StateID() != LadderState.idString && controller.StateID() != JumpState.idString && controller.StateID() != KnockbackState.idString)
					{
						Begin();
					}
				}
				else if(controller.currentState == this)
				{
					if(CanExitCrouch())
					{
						if(willRiseWithButtonRelease)
						{
							ExitCrouch();
						}
						else if(controller.axis.y == 1.0f)
						{
							ExitCrouch();
						}
					}
				}

				if(controller.currentState == this)
				{
					if(canMove && Mathf.Abs(controller.slots.physicsObject.properties.velocity.x) > 0.0f)
					{
						if(substate != Substate.Moving)
						{
							substate = Substate.Moving;
							PlaySecondaryAnimation(movingAnimation);
						}
					}
					else
					{
						if(substate != Substate.Stopped)
						{
							substate = Substate.Stopped;
							PlayAnimation();
						}
					}

					if(controller.axis.x == 0.0f)
					{
						hasPlayerReleasedHorizontal = true;
					}
				}
			}
		}

		public override void PlayAnimationForSubstate()
		{
			switch(substate)
			{
				case Substate.Stopped:
					PlayAnimation();
					break;
				case Substate.Moving:
					PlaySecondaryAnimation(movingAnimation);
					break;
			}
		}

		#region unique public methods

		public bool WillAllowMovement()
		{
			bool willAllowMovement = true;
			if(!canMove)
			{
				willAllowMovement = false;
			}

			if(!hasPlayerReleasedHorizontal && mustReleaseButtonToMove)
			{
				willAllowMovement = false;
			}


			if(mustReleaseButtonToMove && !hasStoppedMovingSinceCrouch)
			{
				willAllowMovement = false;
			}

			if(!isSkidComplete)
			{
				willAllowMovement = false;
			}

			return willAllowMovement;
		}

		public bool CanExitCrouch()
		{
			bool canExit = true;
			Direction.Vertical direction = (Direction.Vertical)controller.GravityScaleMultiplier();

			if(RaycastHelper.IsUnderOverhang(direction, nonCrouchingColliderSize, boxCollider.transform.position))
			{
				canExit = false;
			}

			return canExit;
		}

		#endregion

		#region override public methods

		public override void UpdateMovement()
		{
			if(controller.slots.physicsObject.properties.velocity.x == 0.0f)
			{
				hasStoppedMovingSinceCrouch = true;
			}

			if(controller.slots.physicsObject.properties.velocity.x == 0.0f)
			{
				isSkidComplete = true;
			}

			if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.cancels.crouch)
			{
				if(CanExitCrouch())
				{
					ExitCrouch();
				}
			}
		}

		public override void OnBegin()
		{ 
			if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.canceledBy.onCrouch)
			{
				controller.slots.actor.currentAttack.Cancel();
			}

			if(boxCollider == null)
			{
				GetCollider();
			}

			SetToCrouchingCollider();

			isSkidComplete = false;

			if(immediatelyKillDecelerationOnCrouch)
			{
				controller.slots.physicsObject.SetVelocityX(0.0f);
				isSkidComplete = true;
			}

			if(controller.GetComponent<MovingState>().movementProperties.deceleration == 0.0f)
			{
				isSkidComplete = true;
			}

			if(mustReleaseButtonToMove)
			{
				if(Mathf.Abs(controller.slots.physicsObject.properties.velocity.x) > 0.0f)
				{
					hasStoppedMovingSinceCrouch = false;
				}
				else
				{
					hasStoppedMovingSinceCrouch = true;
				}
			}

			if(!canMove || mustReleaseButtonToMove)
			{
				if(Mathf.Abs(controller.axis.x) > 0.0f)
				{
					hasPlayerReleasedHorizontal = false;
				}
				else
				{
					hasPlayerReleasedHorizontal = true;
				}
			}
		}

		public override void OnStateChanged()
		{
			if(controller.StateID() != DefaultState.idString)
			{
				SetToNonCrouchingCollider();
			}

			if(controller.StateID() == JumpState.idString)
			{
				SetToNonCrouchingCollider();
				End();
			}
		}

		#endregion

		protected void ExitCrouch()
		{
			controller.SetStateToDefault();
			SetToNonCrouchingCollider();
		}

		protected void SetToCrouchingCollider()
		{
			boxCollider.size = colliderSize;
			boxCollider.offset = colliderOffset;
		}

		protected void SetToNonCrouchingCollider()
		{
			boxCollider.size = nonCrouchingColliderSize;
			boxCollider.offset = nonCrouchingColliderOffset;
		}

		protected IEnumerator SetNonCrouchingColliderCoroutine()
		{
			yield return new WaitForEndOfFrame();

			SetToNonCrouchingCollider();
		}

		protected void GetCollider()
		{
			boxCollider = controller.slots.actor.GetComponent<BoxCollider2D>();
			nonCrouchingColliderSize = boxCollider.size;
			nonCrouchingColliderOffset = boxCollider.offset;
		}
	}
}
