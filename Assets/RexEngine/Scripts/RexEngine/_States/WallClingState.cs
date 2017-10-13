/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class WallClingState:RexState
	{
		public enum Substate
		{
			None,
			Stopped,
			Sliding, 
			Climbing,
			LedgeHanging
		}

		public class BufferZoneInfo
		{
			public bool isLedgeDetected = false;
			public bool isInBufferZone = false;
			public float bufferCutoff = 0.0f;
			public float actorSnapPosition;
		}

		[System.Serializable]
		public class Animations
		{
			public AnimationClip climbWallMoving;
			public AnimationClip climbWallStopped;
			public AnimationClip ledgeHang;
		}

		[System.Serializable]
		public class WallJump
		{
			public bool enableWallJump = true;
			public int wallJumpGraceFrames = 3;
			public int wallJumpKickbackFrames = 7;
		}

		[System.Serializable]
		public class WallClimb
		{
			public bool enableClimbing;
			public float climbSpeed = 5.0f;
		}

		[System.Serializable]
		public class LedgeGrab
		{
			public bool enableLedgeGrab;
			public bool canLedgeJump = true;
		}

		public const string idString = "WallCling";

		public WallJump wallJump;
		public WallClimb wallClimb;
		public LedgeGrab ledgeGrab;

		public bool enableClingWhileJumping;
		public bool clingRequiresDirectionalHold = true;
		public bool canDisengageWithDirectionalPress = true;
		public float wallSlideSpeed = 1.0f;
		public bool attacksReverseOnWall = true;
		public Animations animations;

		protected Substate substate;
		protected bool isClingingToWall = false;
		protected int cooldownFrames = 20;
		protected int currentCooldownFrame = 0;
		protected int currentWallJumpGraceFrame = 0;
		protected bool isDropping;
		protected Direction.Horizontal mostRecentWallJumpDirection;
		protected JumpState jumpState;

		void Awake() 
		{
			id = idString;
			isConcurrent = false;
			willPlayAnimationOnBegin = false;
			GetController();
		}

		void Update()
		{
			if(!(isEnabled && controller.isEnabled))
			{
				isClingingToWall = false;
				return;
			}

			CheckForWallCling();
		}

		void FixedUpdate()
		{
			if(!isEnabled || (controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.cancels.wallClinging))
			{
				currentWallJumpGraceFrame = 0;
				currentCooldownFrame = 0;
				isClingingToWall = false;
				return;
			}

			if(controller.StateID() != FallingState.idString && !controller.isKnockbackActive && !controller.isStunned)
			{
				isDropping = false;
			}

			CheckForWallCling();

			bool isPressingIntoWall = IsPressingIntoWall();
			float gravityScale = controller.GravityScaleMultiplier();

			BufferZoneInfo bufferZoneInfo_Top = GetBufferZoneInfo(gravityScale, Direction.Vertical.Down);
			BufferZoneInfo bufferZoneInfo_Bottom = GetBufferZoneInfo(gravityScale, Direction.Vertical.Up);
			if(isPressingIntoWall && IsClingAllowed() && !isDropping)
			{
				if(!(IsJumpActive() && !enableClingWhileJumping))
				{
					if(bufferZoneInfo_Top.isLedgeDetected && !bufferZoneInfo_Top.isInBufferZone)
					{
						if((controller.slots.physicsObject.properties.velocity.y * gravityScale) < 0.0f && controller.StateID() != this.id)
						{
							controller.slots.actor.SetPosition(new Vector2(controller.slots.actor.transform.position.x, bufferZoneInfo_Top.bufferCutoff));

							if(ledgeGrab.enableLedgeGrab)
							{
								if(substate != Substate.LedgeHanging)
								{
									PlaySecondaryAnimation(animations.ledgeHang);
								}

                            	substate = Substate.LedgeHanging;
							}
						}
					}
					else if(bufferZoneInfo_Bottom.isLedgeDetected && !bufferZoneInfo_Bottom.isInBufferZone && wallClimb.enableClimbing)
					{
						if(!(bufferZoneInfo_Top.isLedgeDetected && bufferZoneInfo_Bottom.isLedgeDetected))
						{
							if((controller.slots.physicsObject.properties.velocity.y * gravityScale) > 0.0f && controller.StateID() != this.id)
							{
								controller.slots.actor.SetPosition(new Vector2(controller.slots.actor.transform.position.x, bufferZoneInfo_Bottom.bufferCutoff));
							}
						}
					}

				}
			}

			float climbableArea = GetComponent<BoxCollider2D>().size.y;
			if(bufferZoneInfo_Top.isLedgeDetected && bufferZoneInfo_Bottom.isLedgeDetected)
			{
				climbableArea = (bufferZoneInfo_Top.bufferCutoff - bufferZoneInfo_Bottom.bufferCutoff) * gravityScale;
			}

			if((bufferZoneInfo_Top.isLedgeDetected && bufferZoneInfo_Top.isInBufferZone && controller.slots.physicsObject.properties.velocity.y * gravityScale < 0.0f) || (bufferZoneInfo_Bottom.isLedgeDetected && bufferZoneInfo_Bottom.isInBufferZone && controller.slots.physicsObject.properties.velocity.y * gravityScale >= 0.0f))
			{
				bool isInBothBufferZones = false;
				if(bufferZoneInfo_Top.isLedgeDetected && bufferZoneInfo_Bottom.isLedgeDetected)
				{
					climbableArea = (bufferZoneInfo_Top.bufferCutoff - bufferZoneInfo_Bottom.bufferCutoff) * gravityScale;
					if(climbableArea < 0.0f && ((transform.position.y <= bufferZoneInfo_Top.bufferCutoff && gravityScale > 0.0f) || (transform.position.y >= bufferZoneInfo_Top.bufferCutoff && gravityScale < 0.0f)))
					{
						isInBothBufferZones = true;
					}
				}

				if(!isInBothBufferZones || !IsCooldownComplete() || isDropping) //Ordinarily, disengage from the wall; however, if we're in a buffer zone only because the entire wall is a buffer zone, don't disengage; allow us to stay at the top
				{
					isClingingToWall = false;
				}
			}

			if(wallSlideSpeed == 0.0f && !wallClimb.enableClimbing && substate != Substate.LedgeHanging)
			{
				isClingingToWall = false;
			}

			if(isClingingToWall)
			{
				currentWallJumpGraceFrame = wallJump.wallJumpGraceFrames;
				if(controller.StateID() != idString && !(IsJumpActive() && !enableClingWhileJumping))
				{
					if(controller.StateID() != LadderState.idString && IsCooldownComplete() && !bufferZoneInfo_Top.isInBufferZone)
					{
						if(!(bufferZoneInfo_Bottom.isLedgeDetected && bufferZoneInfo_Bottom.isInBufferZone && wallClimb.enableClimbing) || climbableArea < 0.0f)
						{
							Begin();
							controller.slots.physicsObject.SetVelocityX(0);
						}
					}
				}

				if(controller.currentState == this)
				{

					if(Mathf.Abs(wallSlideSpeed) > 0.0f)
					{
						controller.slots.physicsObject.FreezeGravityForSingleFrame();
						controller.slots.physicsObject.SetVelocityY(wallSlideSpeed * gravityScale * -1.0f);

						if(!wallClimb.enableClimbing && substate != Substate.LedgeHanging && !controller.slots.actor.IsAttacking())
						{
							if(substate != Substate.LedgeHanging)
							{
								PlayAnimation();
							}

							substate = Substate.Sliding;
						}
					}

					if(wallClimb.enableClimbing || substate == Substate.LedgeHanging)
					{
						controller.slots.physicsObject.FreezeGravityForSingleFrame();
						controller.slots.physicsObject.SetVelocityY(0.0f);
					}
				}

				if(wallClimb.enableClimbing && Mathf.Abs(controller.slots.input.verticalAxis) > 0.0f && controller.StateID() == idString)
				{
					float climbVelocity = controller.slots.input.verticalAxis * wallClimb.climbSpeed * gravityScale;
					if(climbableArea < GetComponent<BoxCollider2D>().size.y)
					{
						climbVelocity = 0.0f;
					}

					if(bufferZoneInfo_Top.isLedgeDetected)
					{
						if((gravityScale < 0.0f && transform.position.y + (climbVelocity * PhysicsManager.Instance.fixedDeltaTime) < bufferZoneInfo_Top.bufferCutoff) || (gravityScale > 0.0f && transform.position.y + (climbVelocity * PhysicsManager.Instance.fixedDeltaTime) > bufferZoneInfo_Top.bufferCutoff))
						{
							controller.slots.actor.SetPosition(new Vector2(controller.slots.actor.transform.position.x, bufferZoneInfo_Top.bufferCutoff));
							climbVelocity = 0.0f;

							if(ledgeGrab.enableLedgeGrab)
							{
								if(substate != Substate.LedgeHanging && !controller.slots.actor.IsAttacking())
								{
									PlaySecondaryAnimation(animations.ledgeHang);
								}

								substate = Substate.LedgeHanging;
							}
						}
					}
					else if(bufferZoneInfo_Bottom.isLedgeDetected)
					{
						if((gravityScale < 0.0f && transform.position.y + (climbVelocity * PhysicsManager.Instance.fixedDeltaTime) > bufferZoneInfo_Bottom.bufferCutoff) || (gravityScale > 0.0f && transform.position.y + (climbVelocity * PhysicsManager.Instance.fixedDeltaTime) < bufferZoneInfo_Bottom.bufferCutoff))
						{
							controller.slots.actor.SetPosition(new Vector2(controller.slots.actor.transform.position.x, bufferZoneInfo_Bottom.bufferCutoff));
							climbVelocity = 0.0f;
						}
					}

					if(controller.currentState == this)
					{
						controller.slots.physicsObject.SetVelocityY(climbVelocity);
					}

					if(!(substate == Substate.LedgeHanging && controller.slots.input.verticalAxis > 0.0f))
					{
						if(substate != Substate.Climbing && !controller.slots.actor.IsAttacking())
						{
							PlaySecondaryAnimation(animations.climbWallMoving);
						}

						substate = Substate.Climbing;
					}
				}
				else if(wallClimb.enableClimbing)
				{
					if(!bufferZoneInfo_Top.isInBufferZone && substate != Substate.LedgeHanging && substate != Substate.Sliding)
					{
						if(substate != Substate.Stopped && !controller.slots.actor.IsAttacking())
						{
							PlaySecondaryAnimation(animations.climbWallStopped);
						}

						substate = Substate.Stopped;
					}
				}
			}
			else
			{
				if(controller.StateID() == this.id)
				{
					controller.SetStateToDefault();
				}
			}

			UpdateCooldownFrames();
		}

		#region override public methods

		public override void OnBegin()
		{
			if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.canceledBy.onWallCling)
			{
				controller.slots.actor.currentAttack.Cancel();
			}
		}

		public override void OnNewStateAdded(RexState _state)
		{
			if(_state.id == JumpState.idString && jumpState == null)
			{
				jumpState = _state as JumpState;
			}	
		}

		public override void PlayAnimationForSubstate()
		{
			AnimationClip animationToPlay = animation;
			switch(substate)
			{
				case Substate.Climbing:
					animationToPlay = animations.climbWallMoving;
					break;
				case Substate.Stopped:
					animationToPlay = animations.climbWallStopped;
					break;
				case Substate.Sliding:
					animationToPlay = animation;
					break;
				case Substate.LedgeHanging:
					animationToPlay = animations.ledgeHang;
					break;
			}

			PlaySecondaryAnimation(animationToPlay);
		}

		public override void OnEnded()
		{
			isClingingToWall = false;
			substate = Substate.None;
		}

		public override void OnStateChanged()
		{
			isClingingToWall = false;
			substate = Substate.None;
		}

		#endregion

		public bool IsWallJumpPossible()
		{
			bool isWallJumpPossible = false;
			if(wallJump.enableWallJump && currentWallJumpGraceFrame > 0)
			{
				isWallJumpPossible = true;
			}

			if(substate == Substate.LedgeHanging)
			{
				isWallJumpPossible = true;
			}

			return isWallJumpPossible;
		}

		protected BufferZoneInfo GetBufferZoneInfo(float gravityScale, Direction.Vertical verticalDirection)
		{
			BufferZoneInfo bufferZoneInfo = new BufferZoneInfo();
			bool willSnapToLedgeHeight = wallClimb.enableClimbing || ledgeGrab.enableLedgeGrab;
			if(willSnapToLedgeHeight)
			{
				RaycastHelper.LedgeInfo ledgeInfo = RaycastHelper.DetectLedgeOnWall(controller.direction.horizontal, (Direction.Vertical)((int)verticalDirection * gravityScale), controller.slots.actor.GetComponent<BoxCollider2D>(), controller.slots.physicsObject.properties.velocity.y * PhysicsManager.Instance.fixedDeltaTime * -(int)verticalDirection);
				if(!ledgeInfo.didHit)
				{
					ledgeInfo = RaycastHelper.DetectLedgeOnWall(controller.direction.horizontal, (Direction.Vertical)((int)verticalDirection * gravityScale), controller.slots.actor.GetComponent<BoxCollider2D>(), controller.slots.physicsObject.properties.velocity.y * PhysicsManager.Instance.fixedDeltaTime * -(int)verticalDirection, 0.0f);
				}

				if(ledgeInfo.didHit)
				{
					bufferZoneInfo.isLedgeDetected = true;
					bufferZoneInfo.bufferCutoff = ledgeInfo.hitY - (controller.slots.actor.GetComponent<BoxCollider2D>().size.y * 0.5f * gravityScale * -(int)verticalDirection);
					float adjustedPositionY = transform.position.y + controller.slots.physicsObject.properties.velocity.y * PhysicsManager.Instance.fixedDeltaTime * -(int)verticalDirection;
					if(verticalDirection == Direction.Vertical.Down && ((gravityScale < 0.0f && adjustedPositionY < bufferZoneInfo.bufferCutoff) || (gravityScale > 0.0f && adjustedPositionY > bufferZoneInfo.bufferCutoff))) //At top of wall
					{
						bufferZoneInfo.isInBufferZone = true;
					}
					else if(verticalDirection == Direction.Vertical.Up && ((gravityScale < 0.0f && adjustedPositionY > bufferZoneInfo.bufferCutoff) || (gravityScale > 0.0f && adjustedPositionY < bufferZoneInfo.bufferCutoff)))
					{
						bufferZoneInfo.isInBufferZone = true;
					}
				}
			}

			return bufferZoneInfo;
		}

		protected void UpdateCooldownFrames()
		{
			currentCooldownFrame --;
			if(currentCooldownFrame < 0)
			{
				currentCooldownFrame = 0;
			}

			currentWallJumpGraceFrame --;
			if(currentWallJumpGraceFrame < 0)
			{
				currentWallJumpGraceFrame = 0;
			}
		}

		protected bool IsClingAllowed()
		{
			bool isClingAllowed = true;
			if(!IsCooldownComplete())
			{
				isClingAllowed = false;
			}

			if(isDropping)
			{
				if(!(controller.slots.physicsObject.properties.isAgainstRightWall && controller.axis.x == 1.0f) || (controller.slots.physicsObject.properties.isAgainstLeftWall && controller.axis.x == -1.0f))
				{
					isClingAllowed = false;
				}
			}

			return isClingAllowed;
		}

		protected bool IsPressingIntoWall()
		{
			bool isPressingIntoWall = false;
			if(controller.slots.physicsObject.properties.isAgainstRightWall && controller.direction.horizontal == Direction.Horizontal.Right && (controller.axis.x == 1.0f || !clingRequiresDirectionalHold))
			{
				isPressingIntoWall = true;
			}
			else if(controller.slots.physicsObject.properties.isAgainstLeftWall && controller.direction.horizontal == Direction.Horizontal.Left && (controller.axis.x == -1.0f || !clingRequiresDirectionalHold))
			{
				isPressingIntoWall = true;
			}

			return isPressingIntoWall;
		}

		protected void CheckForWallCling()
		{
			if(controller.StateID() == KnockbackState.idString || controller.isStunned)
			{
				isClingingToWall = false;
				return;
			}

			if(controller.slots.physicsObject.properties.isAgainstRightWall)
			{
				mostRecentWallJumpDirection = Direction.Horizontal.Left;
			}
			else if(controller.slots.physicsObject.properties.isAgainstLeftWall)
			{
				mostRecentWallJumpDirection = Direction.Horizontal.Right;
			}

			isClingingToWall = false;
			if(!controller.slots.physicsObject.IsOnSurface())
			{
				if(IsPressingIntoWall() && IsClingAllowed() && !(controller.StateID() == JumpState.idString && !enableClingWhileJumping))
				{
					isClingingToWall = true;
					currentWallJumpGraceFrame = wallJump.wallJumpGraceFrames;
				}
			}

			if(controller.slots.input.isJumpButtonDownThisFrame)
			{
				bool canJump = false;
				if((((controller.slots.physicsObject.properties.isAgainstRightWall || controller.slots.physicsObject.properties.isAgainstLeftWall)) || currentWallJumpGraceFrame > 0) && wallJump.enableWallJump && (!IsJumpActive() || enableClingWhileJumping) && !controller.slots.physicsObject.IsOnSurface() && substate != Substate.LedgeHanging && IsCooldownComplete())
				{
					canJump = true;
				}
				else if(substate == Substate.LedgeHanging && ledgeGrab.canLedgeJump && !controller.slots.physicsObject.IsOnSurface())
				{
					canJump = true;
				}

				if(canJump)
				{
					if(jumpState == null)
					{
						jumpState = controller.GetComponent<JumpState>();
					}

					if(jumpState)
					{
						jumpState.NotifyOfWallJump(wallJump.wallJumpKickbackFrames, mostRecentWallJumpDirection);
						isClingingToWall = false;
					}
				}
				else if(!(wallJump.enableWallJump && substate != Substate.LedgeHanging) || (substate == Substate.LedgeHanging && !ledgeGrab.canLedgeJump) && (wallClimb.enableClimbing || substate == Substate.LedgeHanging))
				{
					currentCooldownFrame = cooldownFrames;
					isClingingToWall = false;

					if(substate != Substate.None)
					{
						isDropping = true;
					}

					controller.SetStateToDefault();
				}
			}

			if(canDisengageWithDirectionalPress && ((controller.slots.physicsObject.properties.isAgainstRightWall && controller.axis.x == -1.0f) || (controller.slots.physicsObject.properties.isAgainstLeftWall && controller.axis.x == 1.0f)))
			{
				isClingingToWall = false;
				controller.SetStateToDefault();
			}

			if(isClingingToWall)
			{
				isDropping = false;
			}
		}

		protected bool IsJumpActive()
		{
			bool isJumpActive = false;
			if(jumpState == null)
			{
				jumpState = GetComponent<JumpState>();
			}

			if(jumpState)
			{
				isJumpActive = jumpState.IsJumpActive();
			}

			return isJumpActive;
		}

		protected bool IsCooldownComplete()
		{
			return (currentCooldownFrame <= 0);
		}
	}
}
