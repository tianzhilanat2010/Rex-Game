/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class BounceState:RexState 
	{
		public int minFrames = 6;
		public int maxFrames = 15;
		public float speed = 8.0f;
		public int damageDealt = 1;

		public const string idString = "Bouncing";

		protected int currentBounceFrame = 0;
		protected bool isBounceActive;

		void Awake() 
		{
			id = idString;
			isConcurrent = true;
			GetController();
		}

		#region unique public methods

		//Called automatically in collision handling of RexActor
		public void StartBounce(Collider2D bouncerCol, Collider2D otherCol)
		{
			if(!isBounceActive)
			{
				Begin();
				currentBounceFrame = 0;
				controller.slots.physicsObject.properties.isFalling = false;
				isBounceActive = true;
				controller.slots.physicsObject.SetVelocityY(0.0f);

				float newY = transform.position.y; //Adjust our position so we aren't inside the thing we're bouncing on
				float buffer = 0.05f;
				float adjustedDistance = (PhysicsManager.Instance.gravityScale >= 0.0f) ? Mathf.Abs(bouncerCol.bounds.min.y - otherCol.bounds.max.y) + buffer : -Mathf.Abs(otherCol.bounds.min.y - bouncerCol.bounds.max.y) - buffer;
				newY += adjustedDistance;
				controller.slots.actor.SetPosition(new Vector2(transform.position.x, newY));
			}
		}

		//Uses positioning and collider data to determine if we can start a bounce from an object
		public bool CanBounce(Collider2D bouncerCol, Collider2D otherCol)
		{
			bool canBounce = false;
			if(isEnabled && controller.isEnabled && IsColliderBelow(bouncerCol, otherCol) && !controller.slots.physicsObject.IsOnSurface())
			{
				RexActor actorToBounceOn = otherCol.GetComponent<RexActor>();
				if(actorToBounceOn != null && actorToBounceOn.canBounceOn)
				{
					canBounce = true;
				}
			}

			return canBounce;
		}

		#endregion

		#region override public methods

		public override void OnBegin()
		{
			if(GetComponent<JumpState>())
			{
				GetComponent<JumpState>().OnBounce();
			}
		}

		public override void OnEnded()
		{
			isBounceActive = false;
		}

		public override void UpdateMovement()
		{
			if(isBounceActive) //The bounce is active, so we update its movement
			{
				currentBounceFrame ++;
				if(currentBounceFrame >= maxFrames) //End the bounce
				{
					currentBounceFrame = 0;
					isBounceActive = false;
					controller.slots.actor.NotifyOfControllerJumpCresting();
					controller.slots.physicsObject.SetVelocityY(0.0f);
					End();
				}
				else //Continue the bounce
				{
					controller.slots.physicsObject.AddVelocityForSingleFrame(new Vector2(0.0f, speed * controller.GravityScaleMultiplier()));
				}
			}
		}

		public override void OnStateChanged()
		{
			if(controller.StateID() == JumpState.idString)
			{
				isBounceActive = false;
				currentBounceFrame = 0;
				End();
			}
			else if(controller.StateID() == KnockbackState.idString)
			{
				End();
			}
		}

		#endregion

		//Determines if the collider we're trying to bounce on is below us, therefore enabling the bounce (or above, in cases of reverse gravity)
		private bool IsColliderBelow(Collider2D bouncerCol, Collider2D otherCol)
		{
			float otherColliderTopY = (PhysicsManager.Instance.gravityScale >= 0.0f) ? otherCol.bounds.max.y : otherCol.bounds.min.y;
			float colliderBottomY = (PhysicsManager.Instance.gravityScale >= 0.0f) ? bouncerCol.bounds.min.y : bouncerCol.bounds.max.y;

			float buffer = 1.0f;
			if((PhysicsManager.Instance.gravityScale >= 0.0f && colliderBottomY >= otherColliderTopY - buffer) || (PhysicsManager.Instance.gravityScale < 0.0f && colliderBottomY >= otherColliderTopY - buffer))
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
