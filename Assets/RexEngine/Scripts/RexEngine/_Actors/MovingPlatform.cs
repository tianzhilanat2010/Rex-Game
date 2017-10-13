/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RexEngine
{
	public class MovingPlatform:MonoBehaviour 
	{
		public Vector2 moveSpeed = new Vector2(1.0f, 1.0f);
		public bool isMovementEnabled = true;
		public bool willStartWhenPlayerIsOnTop;
		public bool willTurnOnWallContact;
		public bool willTurnOnDistance;
		public Direction.Horizontal startingDirectionX = Direction.Horizontal.Right;
		public Direction.Vertical startingDirectionY = Direction.Vertical.Up;
		public RexPhysics physicsObject;
		public Transform maxMovePosition;
		public Transform minMovePosition;

		[HideInInspector]
		public Vector2 moveDistance;

		protected Vector2 minMovePositionVector;
		protected Vector2 maxMovePositionVector;

		[HideInInspector]
		public bool hasPlayerOnTop;

		protected bool isMoving;
		protected Direction.Horizontal directionX;
		protected Direction.Vertical directionY;

		private Rect colliderRect;

		public Vector2 GetVelocity()
		{
			if(physicsObject)
			{
				return physicsObject.properties.velocity;
			}
			else
			{
				return new Vector2(0.0f, 0.0f);
			}
		}

		void Awake()
		{
			if(!physicsObject)
			{
				physicsObject = GetComponent<RexPhysics>();
			}

			if(minMovePosition)
			{
				minMovePositionVector = new Vector2(minMovePosition.position.x, minMovePosition.position.y);
			}

			if(maxMovePosition)
			{
				maxMovePositionVector = new Vector2(maxMovePosition.position.x, maxMovePosition.position.y);
			}
		}

		void Start()
		{
			directionX = startingDirectionX;
			directionY = startingDirectionY;
			if(!willStartWhenPlayerIsOnTop && isMovementEnabled)
			{
				StartMoving();
			}
		}

		void FixedUpdate()
		{
			if(isMoving && isMovementEnabled)
			{
				MoveHorizontal();
				MoveVertical();
			}

			if(willStartWhenPlayerIsOnTop && hasPlayerOnTop && !isMoving && isMovementEnabled)
			{
				StartMoving();
			}
		}

		public virtual void NotifyOfObjectOnTop()
		{
			hasPlayerOnTop = true;
		}

		protected void StartMoving()
		{
			isMoving = true;
		}

		protected void MoveHorizontal()
		{
			bool willTurn = false;
			if((physicsObject.DidHitEitherWallThisFrame() && willTurnOnWallContact))
			{
				willTurn = true;
			}
			else if(willTurnOnDistance && ((transform.position.x >= maxMovePositionVector.x && directionX == Direction.Horizontal.Right) || (transform.position.x <= minMovePositionVector.x && directionX == Direction.Horizontal.Left)))
			{
				willTurn = true;
				float snappingPosition;
				if(transform.position.x >= maxMovePositionVector.x && directionX == Direction.Horizontal.Right)
				{
					snappingPosition = maxMovePositionVector.x;
				}
				else
				{
					snappingPosition = minMovePositionVector.x;
				}

				physicsObject.previousFrameProperties.position = new Vector3(snappingPosition, transform.position.y, transform.position.z);
				physicsObject.properties.position = new Vector3(snappingPosition, transform.position.y, transform.position.z);
			}

			if(willTurn)
			{
				directionX = (directionX == Direction.Horizontal.Left) ? Direction.Horizontal.Right : Direction.Horizontal.Left;
			}

			physicsObject.SetVelocityX(moveSpeed.x * (int)directionX);
		}

		protected void MoveVertical()
		{
			bool willTurn = false;
			if((physicsObject.IsOnSurface() && willTurnOnWallContact))
			{
				willTurn = true;
			}
			else if(willTurnOnDistance && ((transform.position.y >= maxMovePositionVector.y && directionY == Direction.Vertical.Up) || (transform.position.y <= minMovePositionVector.y && directionY == Direction.Vertical.Down)))
			{
				willTurn = true;
				float snappingPosition;
				if(transform.position.y >= maxMovePositionVector.y && directionY == Direction.Vertical.Up)
				{
					snappingPosition = maxMovePositionVector.y;
				}
				else
				{
					snappingPosition = minMovePositionVector.y;
				}

				physicsObject.previousFrameProperties.position = new Vector3(transform.position.x, snappingPosition, transform.position.z);
				physicsObject.properties.position = new Vector3(transform.position.x, snappingPosition, transform.position.z);
			}

			if(willTurn)
			{
				directionY = (directionY == Direction.Vertical.Up) ? Direction.Vertical.Down : Direction.Vertical.Up;
			}

			physicsObject.SetVelocityY(moveSpeed.y * (int)directionY);
		}
	}

}
