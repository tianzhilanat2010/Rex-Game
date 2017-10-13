/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class MovingState:RexState
	{
		[System.Serializable]
		public class MovementSpeed
		{
			public float speed = 20.0f;
			public float acceleration;
			public float deceleration;
		}

		[System.Serializable]
		public class RunProperties
		{
			public bool canRun = false;
			public bool canInitiateInAir = false;
			public AnimationClip runAnimation;
		}

		public enum MovementAxis
		{
			Horizontal,
			Vertical
		}

		public enum MoveType
		{
			Walking,
			Running
		}

		public const string idString = "Moving";

		public MovementSpeed movementProperties;
		public MovementSpeed runProperties;
		public RunProperties runSettings;
		public bool canMoveVertically; //Whether or not you can move up and down with the up and down inputs
		public bool willMaintainSpeedOnDiagonal = true; //If true, moving diagonally slows both x and y movement to keep your overall speed the same

		protected JumpState jumpState;
		protected MoveType currentMoveType;
		protected bool isSlowingDownFromRun = false;

		void Awake() 
		{
			id = idString;
			doesTurnAnimationHavePriority = true;
			isConcurrent = true;
			willAllowDirectionChange = true;
			jumpState = GetComponent<JumpState>();
			GetController();
		}

		#region override public methods

		public override void OnNewStateAdded(RexState _state)
		{
			if(_state.id == JumpState.idString && jumpState == null)
			{
				jumpState = _state as JumpState;
			}	
		}

		public override void OnStateChanged()
		{
			if(controller.StateID() == LadderState.idString)
			{
				End();
			}
		}

		public override void PlayAnimationForSubstate()
		{
			if(currentMoveType == MoveType.Running && runSettings.runAnimation != null)
			{
				PlaySecondaryAnimation(runSettings.runAnimation);
			}
			else
			{
				PlayAnimation();
			}
		}

		public override void UpdateMovement()
		{
			ApplyMovement(controller.axis.x, MovementAxis.Horizontal);

			if(canMoveVertically)
			{
				ApplyMovement(controller.axis.y, MovementAxis.Vertical);
			}
		}

		public override void OnBegin()
		{
			isSlowingDownFromRun = false;
			currentMoveType = MoveType.Walking;
		}

		public override void OnEnded()
		{
			isSlowingDownFromRun = false;
			currentMoveType = MoveType.Walking;
		}

		#endregion

		protected void ApplyHorizontalJump(JumpState jumpState)
		{
			controller.slots.physicsObject.properties.velocityCap.x = movementProperties.speed * (int)jumpState.direction;
			controller.slots.physicsObject.SetVelocityX(movementProperties.speed * (int)jumpState.direction);
			controller.slots.physicsObject.properties.acceleration.x = 0.0f;
			controller.slots.physicsObject.properties.deceleration.x = 0.0f;
		}

		protected void ApplyMovement(float _inputDirection, MovementAxis movementAxis)
		{
			bool isLockedForAttack = (controller.slots.physicsObject.IsOnSurface()) ? IsLockedForAttack(Attack.ActionType.GroundMoving) : IsLockedForAttack(Attack.ActionType.AirMoving);
			bool isOverriddenByDash = controller.isDashing;
			bool isRunning = false;

			if(runSettings.canRun && controller.slots.input && controller.slots.input.isRunButtonDown)
			{
				if((controller.slots.physicsObject.IsOnSurface() || runSettings.canInitiateInAir) || currentMoveType == MoveType.Running)
				{
					isRunning = true;
					isSlowingDownFromRun = false;

					if(runSettings.runAnimation && currentMoveType != MoveType.Running && Mathf.Abs(_inputDirection) != 0.0f && controller.slots.physicsObject.IsOnSurface() && controller.StateID() == idString)
					{
						PlaySecondaryAnimation(runSettings.runAnimation);
					}

					currentMoveType = MoveType.Running;
				}
				else
				{
					if(currentMoveType == MoveType.Running && Mathf.Abs(controller.slots.physicsObject.properties.velocity.x) > movementProperties.speed)
					{
						isSlowingDownFromRun = true;
						PlayAnimation();
					}

					currentMoveType = MoveType.Walking;
				}
			}
			else
			{
				if(currentMoveType == MoveType.Running && Mathf.Abs(controller.slots.physicsObject.properties.velocity.x) > movementProperties.speed)
				{
					isSlowingDownFromRun = true;
					PlayAnimation();
				}

				currentMoveType = MoveType.Walking;
			}

			if(Mathf.Abs(controller.slots.physicsObject.properties.velocity.x) <= movementProperties.speed)
			{
				isSlowingDownFromRun = false;
			}

			if(isOverriddenByDash || controller.isKnockbackActive || controller.isStunned || controller.StateID() == WallClingState.idString)
			{
				return;
			}

			bool isLockedIntoJump = (jumpState && !controller.slots.physicsObject.IsOnSurface() && jumpState.IsHorizontalMovementFrozen());
			if(isLockedIntoJump)
			{
				ApplyHorizontalJump(jumpState);
				return;
			}

			if(movementProperties == null)
			{
				return;
			}


			float baseSpeed = (isRunning) ? runProperties.speed : movementProperties.speed;
			float baseAcceleration = (isRunning) ? runProperties.acceleration : movementProperties.acceleration;
			float baseDeceleration = (isRunning || isSlowingDownFromRun) ? runProperties.deceleration : movementProperties.deceleration;

			//If we're moving diagonally and willMaintainSpeedOnDiagonal is true, cut our speed in each direction in half to compensate
			float diagonalMultiplier = 0.707f; //Moving in two directions at once is 1.4x faster than just one; therefore, we multiply speed in each direction by 0.707
			bool isMovingDiagonally = (canMoveVertically && willMaintainSpeedOnDiagonal && Mathf.Abs(controller.axis.x) > 0.0f && Mathf.Abs(controller.axis.y) > 0.0f);
			float speed = (isMovingDiagonally) ? baseSpeed * diagonalMultiplier : baseSpeed;
			float acceleration = (isMovingDiagonally) ? baseAcceleration * diagonalMultiplier : baseAcceleration;
			float deceleration = baseDeceleration;

			if(controller.currentState.id == CrouchState.idString)
			{
				CrouchState crouchState = controller.GetComponent<CrouchState>();
				if(crouchState)
				{
					if(crouchState.isSkidComplete && !crouchState.allowAccelerationOnMove)
					{
						acceleration = 0.0f;
						deceleration = 0.0f;
					}

					if(crouchState.isSkidComplete)
					{
						speed = crouchState.moveSpeed;
					}
				}
			}

			if(_inputDirection != 0.0f && !controller.isKnockbackActive && !isLockedForAttack && !controller.IsOveriddenByCrouch() && !isSlowingDownFromRun) 
			{	
				if(movementAxis == MovementAxis.Horizontal) //Let the controller know that we turned around horizontally
				{
					Direction.Horizontal actorDirection = controller.direction.horizontal;
					actorDirection = (_inputDirection > 0.0f) ? Direction.Horizontal.Right : Direction.Horizontal.Left;
					controller.FaceDirection(actorDirection);
				}

				hasEnded = false; //We animate the sprite moving, but only if it's moving horizontally
				if(CanPlayAnimation(movementAxis))
				{
					Begin();
				}

				if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.canceledBy.onMove)
				{
					controller.slots.actor.currentAttack.Cancel();
				}

				if(acceleration != 0.0f) //Accelerate to the top speed
				{
					if(movementAxis == MovementAxis.Horizontal)
					{
						controller.slots.physicsObject.properties.velocityCap.x = speed * _inputDirection;
						controller.slots.physicsObject.properties.acceleration.x = acceleration * _inputDirection;
						controller.slots.physicsObject.properties.deceleration.x = 0;
					}
					else 
					{
						controller.slots.physicsObject.properties.velocityCap.y = speed * _inputDirection;
						controller.slots.physicsObject.properties.acceleration.y = acceleration * _inputDirection;
						controller.slots.physicsObject.properties.deceleration.y = 0;
					}
				}
				else //Immediately go at top speed
				{
					if(movementAxis == MovementAxis.Horizontal)
					{
						controller.slots.physicsObject.properties.velocityCap.x = speed * _inputDirection;
						controller.slots.physicsObject.SetVelocityX(speed * _inputDirection);
						controller.slots.physicsObject.properties.acceleration.x = 0.0f;
						controller.slots.physicsObject.properties.deceleration.x = 0.0f;
					}
					else
					{
						controller.slots.physicsObject.properties.velocityCap.y = speed * _inputDirection;
						controller.slots.physicsObject.SetVelocityY(speed * _inputDirection);
						controller.slots.physicsObject.properties.acceleration.y = 0.0f;
						controller.slots.physicsObject.properties.deceleration.y = 0.0f;
					}
				}
			} 
			else //We can't move directly, but we can apply residual deceleration
			{
				if(deceleration != 0.0f) //Decelerate gradually to a stop
				{
					if(movementAxis == MovementAxis.Horizontal)
					{
						controller.slots.physicsObject.properties.acceleration.x = 0.0f;
						controller.slots.physicsObject.properties.deceleration.x = deceleration;

						if(Mathf.Abs(controller.slots.physicsObject.properties.velocity.x) <= 0.0f && (controller.slots.physicsObject.IsOnSurface() || canMoveVertically))
						{
							if(!hasEnded)
							{
								if(!controller.isKnockbackActive && controller.StateID() != LadderState.idString)
								{
									controller.SetStateToDefault();
								}
							}

							End();
						}
					}
					else
					{
						controller.slots.physicsObject.properties.acceleration.y = 0.0f;
						controller.slots.physicsObject.properties.deceleration.y = deceleration;
					}
				}
				else //Stop immediately
				{
					if(movementAxis == MovementAxis.Horizontal)
					{
						controller.slots.physicsObject.properties.acceleration.x = 0.0f;
						controller.slots.physicsObject.properties.deceleration.x = 0.0f;
						controller.slots.physicsObject.SetVelocityX(0.0f);

						if(!hasEnded)
						{
							if(!controller.isKnockbackActive && controller.StateID() != LadderState.idString && (controller.slots.physicsObject.IsOnSurface() || canMoveVertically) && controller.StateID() != CrouchState.idString)
							{
								//Default if:
								//You are on the ground, AND !canMoveVertically
								//Or, you are in the air 
								controller.SetStateToDefault();
							}
						}

						//Debug.Log("Ending");

						End();
					}
					else
					{
						controller.slots.physicsObject.properties.acceleration.y = 0.0f;
						controller.slots.physicsObject.properties.deceleration.y = 0.0f;
						controller.slots.physicsObject.SetVelocityY(0.0f);
					}
				}

			}
		}

		protected bool CanPlayAnimation(MovementAxis movementAxis)
		{
			if(((controller.slots.physicsObject.IsOnSurface() && !canMoveVertically) || (canMoveVertically && movementAxis == MovementAxis.Horizontal)) && controller.StateID() != id && controller.currentState.id != JumpState.idString && controller.currentState.id != LadderState.idString && controller.currentState.id != CrouchState.idString)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
