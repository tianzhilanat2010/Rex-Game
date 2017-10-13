/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class DashState:RexState 
	{
		public const string idString = "Dashing";

		public float speed;
		public int minFrames;
		public int maxFrames;
		public bool canJump;
		public bool isCanceledByJump;
		public bool isMomentumRetainedOnJump;
		public bool canChangeDirection;
		public bool isCanceledByDirectionChange;
		public bool requireDirectionalHoldToRetainMomentumOnJump;
		public bool canStartDashInAir;
		public bool canDashFromLadders;
		public bool willStopDashUponLanding;
		public int maxAirDashes;
		public bool freezeVerticalMovementOnAirDash;
		public bool isCanceledByWallContact;

		protected int currentAirDash;
		protected int currentFrame;
		protected bool didCurrentDashStartInAir;
		protected bool hasReleasedButtonSinceDash;

		void Awake() 
		{
			id = idString;
			isConcurrent = true;
			GetController();
		}

		void Update()
		{
			if(!IsFrozen())
			{
				willAllowDirectionChange = (controller.isDashing && !canChangeDirection) ? false : true;
				bool isDashAttempted = controller.slots.input && controller.slots.input.isDashButtonDown;
				if(isDashAttempted)
				{
					Begin();
				}
			}
		}

		#region override public methods

		public override bool CanInitiate()
		{
			return (!controller.isDashing && !controller.isStunned && hasReleasedButtonSinceDash && !IsLockedForAttack(Attack.ActionType.Dashing) && !(!canDashFromLadders && controller.StateID() == "Climbing") && (controller.slots.physicsObject.IsOnSurface() || (canStartDashInAir && currentAirDash < maxAirDashes)));
		}

		public override void OnBegin()
		{
			controller.isDashing = true;
			hasReleasedButtonSinceDash = false;
			currentFrame = 0;
			controller.slots.physicsObject.SetVelocityX(speed * (int)controller.direction.horizontal);
			controller.slots.physicsObject.SetAccelerationCapX(speed * (int)controller.direction.horizontal);

			if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.canceledBy.onDash)
			{
				controller.slots.actor.currentAttack.Cancel();
			}

			if(!controller.slots.physicsObject.IsOnSurface())
			{
				currentAirDash ++;
				didCurrentDashStartInAir = true;

				if(freezeVerticalMovementOnAirDash)
				{	
					/*	JumpMovement currentJumpProperties = GetComponent<JumpMovement>();
				if(currentJumpProperties != null)
				{
					currentJumpProperties.OnDash();

				}*/

					//controller.slots.physicsObject.SetVelocityY(0.0f);
					controller.slots.physicsObject.FreezeYMovementForSingleFrame();
				}
			}
			else
			{
				didCurrentDashStartInAir = false;
			}
		}

		public override void OnEnded()
		{
			/*if(freezeVerticalMovementOnAirDash)
			{
				controller.slots.physicsObject.freezeMovementY = false;
			}*/

			controller.slots.physicsObject.SetVelocityX(0.0f);
			controller.isDashing = false;
			currentFrame = 0;
		}

		public override void OnStateChanged()
		{
			if(controller.StateID() == LadderState.idString)
			{
				End();
			}

			if(isCanceledByJump && controller.StateID() == JumpState.idString)
			{
				controller.isDashing = false;
			}

			/*if(controller.StateID() == JumpState.idString && freezeVerticalMovementOnAirDash)
			{
				controller.slots.physicsObject.freezeMovementY = false;
			}*/
		}

		#endregion

		protected void ContinueDash(float _inputDirection)
		{
			if((controller.slots.input && controller.slots.input.isDashButtonDown) || controller.isDashing)
			{
				if(controller.isDashing) //Continue dash
				{
					//Debug.Log("Dash continue");
					currentFrame ++;
					bool willCancelDash = false;

					Direction.Horizontal actorDirection = (_inputDirection > 0.0f) ? Direction.Horizontal.Right : Direction.Horizontal.Left;
					bool didChangeDirection = (actorDirection != controller.direction.horizontal && _inputDirection != 0.0f) ? true : false;
					if(currentFrame >= minFrames && ((controller.slots.input && !controller.slots.input.isDashButtonDown) || !controller.slots.input))
					{
						willCancelDash = true;
					}

					if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.cancels.dash)
					{
						willCancelDash = true;
					}

					if(currentFrame >= maxFrames)
					{
						willCancelDash = true;
					}

					if(canChangeDirection && _inputDirection != 0.0f)
					{
						if(didChangeDirection)
						{
							controller.FaceDirection(actorDirection);
							if(isCanceledByDirectionChange)
							{
								willCancelDash = true;
							}
						}
					}

					if(isMomentumRetainedOnJump && !controller.slots.physicsObject.IsOnSurface())
					{
						if(requireDirectionalHoldToRetainMomentumOnJump && _inputDirection == 0.0f)
						{
							willCancelDash = true;
						}
						else if(didChangeDirection && isCanceledByDirectionChange)
						{
							willCancelDash = true;
						}
						else if(!didCurrentDashStartInAir)
						{
							willCancelDash = false;
						}
					}

					if(willStopDashUponLanding && controller.slots.physicsObject.DidLandThisFrame())
					{
						willCancelDash = true;
					}

					if(controller.slots.actor.currentAttack != null && controller.slots.actor.currentAttack.cancels.dash)
					{
						willCancelDash = true;
					}

					if(isCanceledByWallContact && controller.slots.physicsObject.IsAgainstEitherWall())
					{
						willCancelDash = true;
					}

					if(willCancelDash) //Dash is canceled
					{
						if(!hasEnded && controller.StateID() == id)
						{
							controller.SetStateToDefault();
						}

						End();

						controller.isDashing = false;
						controller.slots.physicsObject.SetVelocityX(0);
					}
					else //Dash continues
					{
						controller.slots.physicsObject.SetVelocityX(speed * (int)controller.direction.horizontal);
						controller.slots.physicsObject.SetAccelerationCapX(speed * (int)controller.direction.horizontal);

						if(freezeVerticalMovementOnAirDash)
						{
							if(controller.isDashing && didCurrentDashStartInAir && !controller.slots.physicsObject.IsOnSurface() && controller.StateID() == idString)
							{
								controller.slots.physicsObject.FreezeYMovementForSingleFrame();
							}
						}
					}
				}
			}
			else
			{
				hasReleasedButtonSinceDash = true;
			}

			if(currentAirDash != 0 && controller.slots.physicsObject.IsOnSurface())
			{
				currentAirDash = 0;
			}
		}

		public override void UpdateMovement()
		{
			ContinueDash(controller.axis.x);
		}
	}

}
