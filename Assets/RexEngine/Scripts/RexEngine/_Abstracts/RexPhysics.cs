/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Reflection;
using System;

namespace RexEngine
{
	public class RexPhysics:MonoBehaviour 
	{
		[System.Serializable]
		public class Gravity
		{
			public float gravity = 1.0f; //An individual setting for the gravity of this particular object. Higher numbers means this has more weight and falls faster.
			public float maxFallSpeed = 15.0f; //The highest speed this can achieve while falling
			public bool usesGravity = true; //If False, this will ignore gravity

			//[HideInInspector] //A lower gravity scale means objects jump higher and fall slower. A negative gravity scale means inverse gravity.
			public float gravityScale = 1.0f; //Don't set this directly; instead, set GravityScale on the PhysicsManager. 
		}

		[System.Serializable] //Properties are meant to be ReadOnly; you can set them via various public methods below
		public class Properties
		{
			public Vector2 velocity;
			public Vector2 externalVelocity;
			public Vector2 velocityCap;
			public Vector2 acceleration;
			public Vector2 deceleration;

			public bool isGrounded;
			public bool isAgainstCeiling;
			public bool isAgainstLeftWall;
			public bool isAgainstRightWall;

			public bool isFalling;

			public Vector2 position;
		}

		public RexObject rexObject; //If a RexObject is slotted here, it will receive notifications for various events from this RexPhysics object
		public Gravity gravitySettings;
		public bool isMovingPlatform; //If True, other RexPhysics objects can ride on top of this
		public bool freezeMovementX; //If True, this object will not move horizontally
		public bool freezeMovementY; //If True, this object will not move vertically
		public bool willSnapToFloorOnStart; //If True, this object will snap to the closest floor in Awake().

		//[HideInInspector]
		public Properties properties;

		[HideInInspector]
		public Properties previousFrameProperties;

		[HideInInspector]
		public bool willStickToMovingPlatforms;

		[HideInInspector]
		public MovingPlatform movingPlatform; //The moving platform this is riding, if any

		public bool willIgnoreTerrain;
		public RaycastAmounts raycastAmounts; //The number of raycasts this uses for collisions; more raycasts is more precise, at the cost of lesser performance

		public bool isEnabled = true;

		private int collisionLayerMask; //The Layers this can collide with
		private int collisionLayerMaskDown; //Use a separate mask for Down collisions to enable one-way platforms
		private Rect box; //Internal use only

		[System.Serializable]
		public class RaycastAmounts
		{
			public int horizontal = 7;
			public int vertical = 5;
			public bool enableDetailedSlopeCollisions;
			public bool disablePhysicsWhenOffCamera = true;
			public bool enableRedundantVerticalCollisions;
		}

		private float slopeDetectionMargin = 1.5f; //Used in detection of slopes

		private BoxCollider2D boxCol;
		private Vector2 singleFrameVelocityAddition;
		private bool isXMovementFrozenForSingleFrame;
		private bool isYMovementFrozenForSingleFrame;
		private bool isGravityFrozenForSingleFrame;
		private float slopeAngleBuffer = 10.0f;

		void Awake()
		{
			previousFrameProperties.position = transform.position;
			properties.position = transform.position;
			collisionLayerMask = 1 << LayerMask.NameToLayer("Terrain");
			collisionLayerMaskDown = collisionLayerMask;
			FlagsHelper.Set(ref collisionLayerMaskDown, 1 << LayerMask.NameToLayer("PassThroughBottom"));

			if(gameObject.tag == "Player")
			{
				AddToCollisions("Boundaries");
			}

			if(rexObject == null)
			{
				rexObject = GetComponent<RexObject>();
			}
		}

		void Start()
		{
			boxCol = GetComponent<BoxCollider2D>();
			slopeDetectionMargin = boxCol.size.y * 0.5f;

			if(willIgnoreTerrain)
			{
				RemoveFromCollisions("Terrain");
			}
		}

		#region public methods

		//Directly sets the X velocity
		public void SetVelocityX(float newVelocity)
		{
			properties.velocity = new Vector2(newVelocity, properties.velocity.y);
		}

		//Directly sets the Y velocity
		public void SetVelocityY(float newVelocity)
		{
			properties.velocity = new Vector2(properties.velocity.x, newVelocity);
		}

		//Add to velocity for a single frame
		public void AddVelocityForSingleFrame(Vector2 _velocityToAdd)
		{
			singleFrameVelocityAddition = new Vector2(singleFrameVelocityAddition.x + _velocityToAdd.x, singleFrameVelocityAddition.y + _velocityToAdd.y);
		}

		public void FreezeXMovementForSingleFrame()
		{
			isXMovementFrozenForSingleFrame = true;
		}

		public void FreezeYMovementForSingleFrame()
		{
			isYMovementFrozenForSingleFrame = true;
		}

		public void FreezeGravityForSingleFrame()
		{
			isGravityFrozenForSingleFrame = true;
		}

		public void ClearSingleFrameVelocity()
		{
			singleFrameVelocityAddition = Vector2.zero;
		}

		//Used for accleration. This is the highest speed the object can accelerate to
		public void SetAccelerationCapX(float velocityCapX)
		{
			properties.velocityCap = new Vector2(velocityCapX, properties.velocityCap.y);
		}

		public void SetAccelerationCapY(float velocityCapY)
		{
			properties.velocityCap = new Vector2(properties.velocityCap.x, velocityCapY);
		}

		//Adds a new Layer this object can collide with
		public void AddToCollisions(string layerName)
		{
			FlagsHelper.Set(ref collisionLayerMask, 1 << LayerMask.NameToLayer(layerName));
			FlagsHelper.Set(ref collisionLayerMaskDown, 1 << LayerMask.NameToLayer(layerName));

			//Handle Terrain and PassThroughBottom at the same time for ease of use
			if(layerName == "Terrain")
			{
				FlagsHelper.Set(ref collisionLayerMaskDown, 1 << LayerMask.NameToLayer("PassThroughBottom"));
			}
		}

		//Removes a Layer that this object could collide with
		public void RemoveFromCollisions(string layerName)
		{
			FlagsHelper.Unset(ref collisionLayerMask, 1 << LayerMask.NameToLayer(layerName));
			FlagsHelper.Unset(ref collisionLayerMaskDown, 1 << LayerMask.NameToLayer(layerName));

			//Handle Terrain and PassThroughBottom at the same time for ease of use
			if(layerName == "Terrain")
			{
				FlagsHelper.Unset(ref collisionLayerMaskDown, 1 << LayerMask.NameToLayer("PassThroughBottom"));
			}
		}

		//Disables this object from colliding with one-way platforms
		public void DisableOneWayPlatforms()
		{
			FlagsHelper.Unset(ref collisionLayerMaskDown, 1 << LayerMask.NameToLayer("PassThroughBottom"));
		}

		//Enables collision with one-way platforms
		public void EnableOneWayPlatforms()
		{
			FlagsHelper.Set(ref collisionLayerMaskDown, 1 << LayerMask.NameToLayer("PassThroughBottom"));
		}

		//Immediately anchors this object to the nearest floor
		public void AnchorToFloor()
		{
			box = GetRectAtPosition(properties.position);
			Vector2 startPoint = new Vector2(box.xMin, box.center.y);
			Vector2 endPoint = new Vector2(box.xMax, box.center.y);

			RaycastHit2D[] raycastHits = new RaycastHit2D[raycastAmounts.vertical];
			int numberOfCollisions = 0;
			float fraction = 0;

			float rayLength = box.height / 2 + 200.0f;

			float lowestFraction = Mathf.Infinity;
			int savedIndex = 0;

			for(int i = 0; i < raycastAmounts.vertical; i ++) 
			{
				float raycastSpacing = (float)i / (float)(raycastAmounts.vertical - 1);
				Vector2 rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);
				Vector2 rayDirection = -Vector2.up;
				raycastHits[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMaskDown);

				if(raycastHits[i].fraction > 0) 
				{
					properties.isGrounded = true;
					properties.position += Vector2.down * (raycastHits[savedIndex].fraction * rayLength - box.height / 2);
					break;
				}
			}
		}

		//Immediately snaps this object to the nearest wall
		public void SnapToNearestWall(Direction.Horizontal _direction)
		{
			properties.isAgainstLeftWall = false;
			properties.isAgainstRightWall = false;

			float buffer = 0.25f;
			Vector2 startPoint = new Vector2(box.center.x, box.yMin - buffer);
			Vector2 endPoint = new Vector2(box.center.x, box.yMax + buffer);

			RaycastHit2D[] raycastHits = new RaycastHit2D[raycastAmounts.horizontal];
			int numberOfCollisions = 0;
			float fraction = 0;

			float rayVelocity = (_direction == Direction.Horizontal.Left) ? -1.0f : 1.0f;

			float sideRayLength = box.width / 2 + Mathf.Abs(rayVelocity * PhysicsManager.Instance.fixedDeltaTime);
			Vector2 direction = rayVelocity > 0 ? Vector3.right : -Vector3.right;
			bool didCollide = false;

			for(int i = 0; i < raycastAmounts.horizontal; i ++) 
			{
				float raycastSpacing = (float)i / (float)(raycastAmounts.horizontal - 1);
				Vector2 rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);

				raycastHits[i] = Physics2D.Raycast(rayOrigin, direction, sideRayLength, collisionLayerMask);
				if(raycastHits[i].fraction > 0) 
				{
					didCollide = true;

					if(fraction > 0) 
					{
						float slopeAngle = Vector2.Angle(raycastHits[i].point - raycastHits[i - 1].point, Vector2.right);
						if(Mathf.Abs(slopeAngle - 90) < slopeAngleBuffer) 
						{
							if(direction == Vector2.right)
							{
								properties.isAgainstRightWall = true;
							}
							else
							{
								properties.isAgainstLeftWall = true;
							}

							properties.position += (direction * (raycastHits[i].fraction * sideRayLength - box.width / 2.0f));

							properties.velocity = new Vector2(0.0f, properties.velocity.y);
							break;
						}
					}

					numberOfCollisions ++;
					fraction = raycastHits[i].fraction;
				}
			}
		}

		public void SyncGravityScale() //Called automatically by PhysicsManager when its gravityScale is changed
		{
			float newGravityScale = PhysicsManager.Instance.gravityScale;
			if(newGravityScale != gravitySettings.gravityScale && rexObject != null)
			{
				rexObject.OnGravityScaleChanged(newGravityScale);
			}

			gravitySettings.gravityScale = newGravityScale;
		}

		public float GravityScale()
		{
			return gravitySettings.gravityScale;
		}

		public bool IsOnSurface() //Returns True if on the floor in normal gravity, or on the ceiling in reverse gravity
		{
			return (properties.isGrounded && gravitySettings.gravityScale > 0.0f) || (properties.isAgainstCeiling && gravitySettings.gravityScale < 0.0f);
		}

		public bool DidLandThisFrame()
		{
			return ((properties.isGrounded && !previousFrameProperties.isGrounded && gravitySettings.gravityScale > 0.0f) || (properties.isAgainstCeiling && !previousFrameProperties.isAgainstCeiling && gravitySettings.gravityScale <= 0.0f));
		}

		public bool DidHitCeilingThisFrame()
		{
			return ((properties.isGrounded && !previousFrameProperties.isGrounded && gravitySettings.gravityScale < 0.0f) || (properties.isAgainstCeiling && !previousFrameProperties.isAgainstCeiling && gravitySettings.gravityScale >= 0.0f));
			//return (properties.isAgainstCeiling && !previousFrameProperties.isAgainstCeiling);
		}

		public bool DidHitLeftWallThisFrame()
		{
			return (properties.isAgainstLeftWall && !previousFrameProperties.isAgainstLeftWall);
		}

		public bool DidHitRightWallThisFrame()
		{
			return (properties.isAgainstRightWall && !previousFrameProperties.isAgainstRightWall);
		}

		public bool DidHitEitherWallThisFrame()
		{
			return (DidHitLeftWallThisFrame() || DidHitRightWallThisFrame());
		}

		public bool IsAgainstEitherWall()
		{
			return (properties.isAgainstLeftWall || properties.isAgainstRightWall);
		}

		//Sets our current properties to our previous properties
		public void ResetFlags()
		{
			previousFrameProperties.velocity = properties.velocity;
			previousFrameProperties.externalVelocity = properties.externalVelocity;
			previousFrameProperties.velocityCap = properties.velocityCap;
			previousFrameProperties.acceleration = properties.acceleration;
			previousFrameProperties.deceleration = properties.deceleration;

			previousFrameProperties.isGrounded = properties.isGrounded;
			previousFrameProperties.isAgainstCeiling = properties.isAgainstCeiling;
			previousFrameProperties.isAgainstLeftWall = properties.isAgainstLeftWall;
			previousFrameProperties.isAgainstRightWall = properties.isAgainstRightWall;

			previousFrameProperties.isFalling = properties.isFalling;

			previousFrameProperties.position = properties.position;
		}

		//The meat of RexPhysics. Moves the object and handles colliding with surfaces
		public void StepPhysics()
		{
			box = GetRectAtPosition(properties.position);
			bool isOnCamera = CameraHelper.CameraContainsPoint(transform.position, 6.0f);
			bool willStepPhysics = (!isOnCamera && raycastAmounts.disablePhysicsWhenOffCamera) ? false : true;

			if(willStepPhysics)
			{
				TranslateForMovingPlatform();

				if(Mathf.Abs(properties.acceleration.x) > 0.0f && Mathf.Abs(properties.velocityCap.x) > 0.0f)
				{
					ApplyAccelerationX();
				}

				ApplyDecelerationX();

				CheckHorizontalCollisions((properties.velocity.x + properties.externalVelocity.x) * PhysicsManager.Instance.fixedDeltaTime);

				ApplyHorizontalVelocity();

				if(willSnapToFloorOnStart)
				{
					AnchorToFloor();
					willSnapToFloorOnStart = false;
				}
				else
				{
					if(gravitySettings.usesGravity && !isGravityFrozenForSingleFrame)
					{
						ApplyGravity();
					}
					else
					{
						if(Mathf.Abs(properties.acceleration.x) > 0.0f && Mathf.Abs(properties.velocityCap.x) > 0.0f)
						{
							ApplyAccelerationY();
						}

						ApplyDecelerationY();
					}

					CheckVerticalCollisions((properties.velocity.y + properties.externalVelocity.y) * PhysicsManager.Instance.fixedDeltaTime);

					if(raycastAmounts.enableDetailedSlopeCollisions)
					{
						CheckForSlopesInOtherDirection();
					}

					ApplyVerticalVelocity();
				}
			}

			properties.externalVelocity = Vector2.zero;
			singleFrameVelocityAddition = Vector2.zero;

			isXMovementFrozenForSingleFrame = false;
			isYMovementFrozenForSingleFrame = false;
			isGravityFrozenForSingleFrame = false;
		}

		#endregion

		#region private methods

		private void ResetCollider()
		{
			BoxCollider2D tempBoxCol = GetComponent<BoxCollider2D>();
			if(tempBoxCol != null)
			{
				boxCol = GetComponent<BoxCollider2D>();
			}

			slopeDetectionMargin = boxCol.size.y * 0.5f;
		}

		//Called internally; applies acceleration for this frame
		private void ApplyAccelerationX()
		{
			float newVelocityX = properties.velocity.x + properties.acceleration.x;
			if(properties.velocityCap.x > 0)
			{
				if(newVelocityX > properties.velocityCap.x)
				{
					newVelocityX = properties.velocityCap.x;
				}
			}
			else if(properties.velocityCap.x < 0)
			{
				if(newVelocityX < properties.velocityCap.x)
				{
					newVelocityX = properties.velocityCap.x;
				}
			}

			properties.velocity = new Vector2(newVelocityX, properties.velocity.y);
		}

		//Called internally; applies acceleration for this frame
		private void ApplyAccelerationY()
		{
			float newVelocityY = properties.velocity.y + properties.acceleration.y;
			if(properties.velocityCap.y > 0)
			{
				if(newVelocityY > properties.velocityCap.y)
				{
					newVelocityY = properties.velocityCap.y;
				}
			}
			else if(properties.velocityCap.y < 0)
			{
				if(newVelocityY < properties.velocityCap.y)
				{
					newVelocityY = properties.velocityCap.y;
				}
			}

			properties.velocity = new Vector2(properties.velocity.x, newVelocityY);
		}

		//Called internally; applies deceleration for this frame
		private void ApplyDecelerationX()
		{
			float newVelocityX = 0;
			if(properties.velocity.x > 0)
			{
				newVelocityX = properties.velocity.x - properties.deceleration.x;
				if(newVelocityX < 0)
				{
					newVelocityX = 0;
				}
			}
			else if(properties.velocity.x < 0)
			{
				newVelocityX = properties.velocity.x + properties.deceleration.x;
				if(newVelocityX > 0)
				{
					newVelocityX = 0;
				}
			}

			SetVelocityX(newVelocityX);
		}

		//Called internally; applies deceleration for this frame
		private void ApplyDecelerationY()
		{
			float newVelocityY = 0;
			if(properties.velocity.y > 0)
			{
				newVelocityY = properties.velocity.y - properties.deceleration.y;
				if(newVelocityY < 0)
				{
					newVelocityY = 0;
				}
			}
			else if(properties.velocity.y < 0)
			{
				newVelocityY = properties.velocity.y + properties.deceleration.y;
				if(newVelocityY > 0)
				{
					newVelocityY = 0;
				}
			}

			SetVelocityY(newVelocityY);
		}

		//Called internally; applies gravity this frame
		private void ApplyGravity()
		{
			if(Mathf.Abs(singleFrameVelocityAddition.y) > 0.0f)
			{
				properties.velocity = new Vector2(properties.velocity.x, singleFrameVelocityAddition.y - (gravitySettings.gravity * gravitySettings.gravityScale));
			}
			else
			{
				float currentFallSpeed = (gravitySettings.gravityScale >= 0.0f) ? Mathf.Max(-gravitySettings.maxFallSpeed, properties.velocity.y - (gravitySettings.gravity * gravitySettings.gravityScale)) : Mathf.Min(gravitySettings.maxFallSpeed, properties.velocity.y - (gravitySettings.gravity * gravitySettings.gravityScale));
				properties.velocity = new Vector2(properties.velocity.x, currentFallSpeed);
			}

			if((properties.velocity.y < 0 && gravitySettings.gravityScale > 0 && !properties.isGrounded) || (properties.velocity.y > 0 && gravitySettings.gravityScale < 0 && !properties.isAgainstCeiling)) 
			{
				properties.isFalling = true;
			}
		}

		//Checks our horizontal collisions, and either stops us from moving or enables us to continue moving
		private void CheckHorizontalCollisions(float velocityToCheck, bool isTranslatingDirectly = false)
		{
			properties.isAgainstLeftWall = false;
			properties.isAgainstRightWall = false;
			box = GetRectAtPosition(properties.position);
			if(velocityToCheck != 0) 
			{			
				//edgebuffer prevents unintended horizontal collisions with a MovingPlatform you're riding on top of
				float edgeBuffer = (isTranslatingDirectly) ? 0.05f : 0.0f;
				Vector2 startPoint = new Vector2(box.center.x, box.center.y - box.height * 0.5f + edgeBuffer);
				Vector2 endPoint = new Vector2(box.center.x, box.center.y + box.height * 0.5f);

				RaycastHit2D[] raycastHits = new RaycastHit2D[raycastAmounts.horizontal];
				int numberOfCollisions = 0;
				float fraction = 0;

				float sideRayLength = box.width / 2 + Mathf.Abs(velocityToCheck);
				Vector2 direction = (velocityToCheck) > 0 ? Vector2.right : -Vector2.right;
				bool didCollide = false;

				for(int i = 0; i < raycastAmounts.horizontal; i ++) 
				{
					float raycastSpacing = (float)i / (float)(raycastAmounts.horizontal - 1);
					Vector2 rayOrigin;

					if(i == 1 && !willStickToMovingPlatforms)
					{
						rayOrigin = Vector2.Lerp(startPoint, endPoint, ((float)(i - 1) / (float)(raycastAmounts.horizontal - 1)) + 0.005f);
					}
					else if (i == raycastAmounts.horizontal - 2 && !willStickToMovingPlatforms)
					{
						rayOrigin = Vector2.Lerp(startPoint, endPoint, ((float)(i + 1) / (float)(raycastAmounts.horizontal - 1)) - 0.005f);
					}
					else
					{
						rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);
					}

					raycastHits[i] = Physics2D.Raycast(rayOrigin, direction, sideRayLength, collisionLayerMask);
					//Debug.DrawRay(rayOrigin, direction * sideRayLength, Color.red);

					if(raycastHits[i].fraction > 0) 
					{
						didCollide = true;
						if(fraction > 0) 
						{
							float slopeAngle = Vector2.Angle(raycastHits[i].point - raycastHits[i - 1].point, Vector2.right);
							if(Mathf.Abs(slopeAngle - 90) < slopeAngleBuffer) //If the slope is too steep, treat it as a wall
							{
								if(direction == Vector2.right)
								{
									properties.isAgainstRightWall = true;
								}
								else
								{
									properties.isAgainstLeftWall = true;
								}

								if(willStickToMovingPlatforms)
								{
									MovingPlatform platform = raycastHits[i].collider.GetComponent<MovingPlatform>();
									if(platform != null)
									{
										movingPlatform = platform;
									}
									else
									{
										movingPlatform = null;
									}
								}

								properties.position += (direction * (raycastHits[i].fraction * sideRayLength - box.width / 2.0f));
								properties.velocity = new Vector2(0, properties.velocity.y);
								properties.externalVelocity = new Vector2(0, properties.externalVelocity.y);

								RexObject.Side side = (direction == Vector2.right) ? RexObject.Side.Right : RexObject.Side.Left;
								if(side == RexObject.Side.Left && DidHitLeftWallThisFrame() && velocityToCheck < 0.0f)
								{
									NotifyOfCollision(raycastHits[i].collider, RexObject.Side.Left, RexObject.CollisionType.Enter);
								}
								else if(side == RexObject.Side.Right && DidHitRightWallThisFrame() && velocityToCheck > 0.0f)
								{
									NotifyOfCollision(raycastHits[i].collider, RexObject.Side.Right, RexObject.CollisionType.Enter);
								}

								break;
							}
						}

						numberOfCollisions ++;
						fraction = raycastHits[i].fraction;
					}

					if(!didCollide && isTranslatingDirectly)
					{
						properties.position = new Vector2(transform.position.x + velocityToCheck, properties.position.y);
					}

				}
			}
			else
			{
				CheckForWallContact(Direction.Horizontal.Left);
				CheckForWallContact(Direction.Horizontal.Right);
			}
		}

		//Checks in a direction to see if we hit a wall
		private bool CheckForWallContact(Direction.Horizontal _direction, bool willDetectWithMargin = false)
		{
			Vector2 startPoint = new Vector2(box.center.x, box.yMin);
			Vector2 endPoint = new Vector2(box.center.x, box.yMax);

			RaycastHit2D[] raycastHits = new RaycastHit2D[raycastAmounts.horizontal];
			int numberOfCollisions = 0;
			float fraction = 0;

			float margin = (willDetectWithMargin) ? 0.25f : 0.001f;
			float sideRayLength = box.width / 2 + margin;
			Vector2 direction = new Vector2((int)_direction, 0.0f);
			bool didCollide = false;

			for(int i = 0; i < raycastAmounts.horizontal; i ++) 
			{
				float raycastSpacing = (float)i / (float)(raycastAmounts.horizontal - 1);
				Vector2 rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);

				raycastHits[i] = Physics2D.Raycast(rayOrigin, direction, sideRayLength, collisionLayerMask);
				if(raycastHits[i].fraction > 0) 
				{
					didCollide = true;
					if(fraction > 0) 
					{
						float slopeAngle = Vector2.Angle(raycastHits[i].point - raycastHits[i - 1].point, Vector2.right);
						if(Mathf.Abs(slopeAngle - 90) < slopeAngleBuffer) //If the slope is too steep, treat it as a wall
						{
							if(direction == Vector2.right)
							{
								properties.isAgainstRightWall = true;
							}
							else
							{
								properties.isAgainstLeftWall = true;
							}

							return true;
						}
					}

					numberOfCollisions ++;
					fraction = raycastHits[i].fraction;
				}
			}

			return false;
		}

		//Checks to see if we hit either the floor or the ceiling
		private bool CheckForCeilingFloorContact(Direction.Vertical _direction, bool willDetectWithMargin = false)
		{
			float edgeBuffer = 0.025f;
			Vector2 startPoint = new Vector2(box.xMin + edgeBuffer, box.center.y);
			Vector2 endPoint = new Vector2(box.xMax - edgeBuffer, box.center.y);

			RaycastHit2D[] raycastHits = new RaycastHit2D[raycastAmounts.vertical];
			int numberOfCollisions = 0;
			float fraction = 0;

			float margin = (willDetectWithMargin) ? 0.25f : 0.001f;
			float rayLength = box.height / 2 + margin;
			Vector3 direction = new Vector3(0.0f, (int)_direction, 0.0f);
			bool didCollide = false;

			float lowestFraction = Mathf.Infinity;
			int savedIndex = 0;

			for(int i = 0; i < raycastAmounts.vertical; i ++) 
			{
				float raycastSpacing = (float)i / (float)(raycastAmounts.vertical - 1);
				Vector2 rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);
				Vector2 rayDirection = (_direction == Direction.Vertical.Down) ? -Vector2.up : Vector2.up;
				int mask = (_direction == Direction.Vertical.Down) ? collisionLayerMaskDown : collisionLayerMask;
				raycastHits[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, mask);
				if(raycastHits[i].fraction > 0) 
				{
					didCollide = true;
					if(_direction == Direction.Vertical.Up)
					{
						properties.isAgainstCeiling = true;

						return true;
					}
					else if(_direction == Direction.Vertical.Down)
					{
						properties.isGrounded = true;
						willSnapToFloorOnStart = false;
						return true;
					}

					break;
				}
			}

			return false;
		}

		//Checks our vertical collisions, and either stops us from moving or enables us to continue moving
		private void CheckVerticalCollisions(float velocityToCheck, bool isTranslatingDirectly = false)
		{
			if(!willStickToMovingPlatforms)
			{
				movingPlatform = null;
			}

			box = GetRectAtPosition(properties.position);

			Direction.Vertical direction = (velocityToCheck <= 0.0f) ? Direction.Vertical.Down : Direction.Vertical.Up;

			if(velocityToCheck == 0.0f)
			{
				return;
			}

			//The edge buffer prevents you from standing on a ledge with the very corner of your hitbox
			//This fixes problems where you jump straight up *next to* a ledge and the end up resting on the ledge itself on the way down
			float edgeBuffer = 0.025f;
			Vector2 startPoint = new Vector2(box.xMin + edgeBuffer, box.center.y);
			Vector2 endPoint = new Vector2(box.xMax - edgeBuffer, box.center.y);
			RaycastHit2D[] raycastHits = new RaycastHit2D[raycastAmounts.vertical];

			float rayLength = box.height / 2.0f + (((properties.isGrounded && direction == Direction.Vertical.Down) || (properties.isAgainstCeiling && direction == Direction.Vertical.Up)) ? slopeDetectionMargin : Mathf.Abs(velocityToCheck));
			float lowestFraction = Mathf.Infinity;
			int savedIndex = 0;

			bool didCollide = false;
			for(int i = 0; i < raycastAmounts.vertical; i ++) 
			{
				float raycastSpacing = (float)i / (float)(raycastAmounts.vertical - 1);
				Vector2 rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);
				Vector2 rayDirection = (direction == Direction.Vertical.Down) ? -Vector2.up : Vector2.up;
				//Debug.DrawRay(rayOrigin, rayDirection * rayLength, Color.red);

				int mask = ((direction == Direction.Vertical.Down && gravitySettings.gravityScale > 0.0f) || (direction == Direction.Vertical.Up && gravitySettings.gravityScale <= 0.0f)) ? collisionLayerMaskDown : collisionLayerMask;
				
				raycastHits[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, mask);
				if(raycastHits[i].fraction > 0) 
				{
					didCollide = true;
					if(raycastHits[i].fraction < lowestFraction) 
					{
						savedIndex = i;
						lowestFraction = raycastHits[i].fraction;
					}
				}
			}

			//This prevents you from walking off a one-way platform, holding left or right to be back inside it as you fall, and clipping back up on top of it
			if(didCollide && raycastHits[savedIndex].collider.gameObject.layer == LayerMask.NameToLayer("PassThroughBottom"))
			{
				float buffer = 0.1f;
				if((gravitySettings.gravityScale > 0.0f && box.yMin < raycastHits[savedIndex].collider.bounds.max.y - buffer) || (gravitySettings.gravityScale <= 0.0f && box.yMax > raycastHits[savedIndex].collider.bounds.min.y + buffer))
				{
					didCollide = false;
				}
			}

			//In rare instances, if the actor is already on top of a PassThroughBottom collider when they land on the ground, the above raycast can fail to detect the terrain; this is a failsafe
			if(!didCollide && raycastAmounts.enableRedundantVerticalCollisions)
			{
				int adjustedVerticalRaycasts = Mathf.CeilToInt(raycastAmounts.vertical * 0.5f);
				for(int i = 0; i < adjustedVerticalRaycasts; i ++) 
				{
					float raycastSpacing = (float)i / (float)(adjustedVerticalRaycasts - 1);
					Vector2 rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);
					Vector2 rayDirection = (direction == Direction.Vertical.Down) ? -Vector2.up : Vector2.up;
					int mask = collisionLayerMask;
					raycastHits[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, mask);
					if(raycastHits[i].fraction > 0) 
					{
						didCollide = true;
						if(raycastHits[i].fraction < lowestFraction) 
						{
							savedIndex = i;
							lowestFraction = raycastHits[i].fraction;
						}
					}
				}
			}

			if(didCollide) 
			{
				properties.velocity = new Vector2(properties.velocity.x, 0);
				properties.externalVelocity = new Vector2(properties.externalVelocity.x, 0);

				if(direction == Direction.Vertical.Down)
				{
					properties.isGrounded = true; 
					willSnapToFloorOnStart = false;
					properties.isFalling = false;

					properties.isAgainstCeiling = false;
					properties.position += Vector2.down * (raycastHits[savedIndex].fraction * rayLength - box.height / 2);

					MovingPlatform platform = raycastHits[savedIndex].collider.GetComponent<MovingPlatform>();
					if(platform != null)
					{
						movingPlatform = platform;
						movingPlatform.NotifyOfObjectOnTop();
					}
					else
					{
						movingPlatform = null;
					}
				}
				else
				{
					if(willStickToMovingPlatforms)
					{
						MovingPlatform platform = raycastHits[savedIndex].collider.GetComponent<MovingPlatform>();
						if(platform != null)
						{
							movingPlatform = platform;
						}
						else
						{
							movingPlatform = null;
						}
					}

					properties.isAgainstCeiling = true;
					if(gravitySettings.gravityScale > 0.0f) //Hit your head on the ceiling; stop your jump
					{
						properties.isFalling = true;
					}
					else
					{
						properties.isFalling = false;
					}

					properties.position += Vector2.up * (raycastHits[savedIndex].fraction * rayLength - box.height / 2);
				}

				if(DidLandThisFrame())
				{
					if(gravitySettings.gravityScale >= 0.0f)
					{
						NotifyOfCollision(raycastHits[savedIndex].collider, RexObject.Side.Bottom, RexObject.CollisionType.Enter);
					}
					else
					{
						NotifyOfCollision(raycastHits[savedIndex].collider, RexObject.Side.Top, RexObject.CollisionType.Enter);
					}
				}

				if(DidHitCeilingThisFrame())
				{
					if(gravitySettings.gravityScale >= 0.0f)
					{
						NotifyOfCollision(raycastHits[savedIndex].collider, RexObject.Side.Top, RexObject.CollisionType.Enter);
					}
					else
					{
						NotifyOfCollision(raycastHits[savedIndex].collider, RexObject.Side.Bottom, RexObject.CollisionType.Enter);
					}
				}
			} 
			else 
			{
				properties.isAgainstCeiling = false;
				properties.isGrounded = false;

				if(isTranslatingDirectly)
				{
					properties.position = new Vector2(transform.position.x, properties.position.y + velocityToCheck);
				}
			}
		}

		//If we have a rexObject slotted, this will notify it of any collisions and enable it to act accordingly
		private void NotifyOfCollision(Collider2D col, RexObject.Side side, RexObject.CollisionType collisionType)
		{
			if(rexObject != null)
			{
				rexObject.OnPhysicsCollision(col, side, collisionType);
			}

			RexObject otherObject = col.GetComponent<RexObject>();
			if(otherObject != null)
			{
				RexObject.Side otherSide;
				if(side == RexObject.Side.Bottom)
				{
					otherSide = RexObject.Side.Top;
				}
				else if(side == RexObject.Side.Top)
				{
					otherSide = RexObject.Side.Bottom;
				}
				else if(side == RexObject.Side.Left)
				{
					otherSide = RexObject.Side.Right;
				}
				else
				{
					otherSide = RexObject.Side.Left;
				}

				otherObject.NotifyOfCollisionWithPhysicsObject(boxCol, otherSide, collisionType);
			}
		}

		//Used internally for added safety when checking slope collisions
		private void CheckForSlopesInOtherDirection()
		{
			box = GetRectAtPosition(properties.position);
			Direction.Vertical direction = (properties.velocity.y + properties.externalVelocity.y <= 0.0f) ? Direction.Vertical.Up : Direction.Vertical.Down;

			if(Mathf.Abs(properties.velocity.y + properties.externalVelocity.y) == 0.0f)
			{
				return;
			}

			float edgeBuffer = 0.025f;
			Vector2 startPoint = new Vector2(box.xMin + edgeBuffer, box.center.y);
			Vector2 endPoint = new Vector2(box.xMax - edgeBuffer, box.center.y);
			RaycastHit2D[] raycastHits = new RaycastHit2D[raycastAmounts.vertical];

			float rayLength = box.height / 2.0f +  slopeDetectionMargin;
			float lowestFraction = Mathf.Infinity;
			int savedIndex = 0;

			bool didCollide = false;
			for(int i = 0; i < raycastAmounts.vertical; i ++) 
			{
				float raycastSpacing = (float)i / (float)(raycastAmounts.vertical - 1);
				Vector2 rayOrigin = Vector2.Lerp(startPoint, endPoint, raycastSpacing);
				Vector2 rayDirection = (direction == Direction.Vertical.Down) ? -Vector2.up : Vector2.up;

				raycastHits[i] = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMask);
				if(raycastHits[i].fraction > 0) 
				{
					didCollide = true;
					if(raycastHits[i].fraction < lowestFraction) 
					{
						savedIndex = i;
						lowestFraction = raycastHits[i].fraction;
					}
				}
			}

			if(didCollide) 
			{
				if(direction == Direction.Vertical.Down)
				{
					Vector2 tempNewPosition = properties.position + Vector2.down * (raycastHits[savedIndex].fraction * rayLength - box.height / 2);
					if(properties.position.y < tempNewPosition.y)
					{
						properties.position += Vector2.down * (raycastHits[savedIndex].fraction * rayLength - box.height / 2);
					}
				}
				else
				{
					Vector2 tempNewPosition = properties.position + Vector2.up * (raycastHits[savedIndex].fraction * rayLength - box.height / 2);
					if(properties.position.y > tempNewPosition.y)
					{
						properties.position += Vector2.up * (raycastHits[savedIndex].fraction * rayLength - box.height / 2);
					}
				}
			} 
		}

		//if we're riding a moving platform, this keeps us on top of it
		private void TranslateForMovingPlatform()
		{
			if(movingPlatform != null)
			{
				properties.externalVelocity.x = movingPlatform.GetVelocity().x; //For X, we use externalVelocity, to ensure that we properly collide with any walls while on the platform
				properties.position.y += movingPlatform.moveDistance.y; //For Y, we use a direct translation, or else gravity can potentially mess up the calculations
			}
		}

		//At the end of a frame, moves us the appropriate amount on the X axis
		private void ApplyHorizontalVelocity()
		{
			if(!freezeMovementX && !isXMovementFrozenForSingleFrame)
			{
				properties.position = new Vector2(properties.position.x + ((properties.velocity.x + properties.externalVelocity.x) * PhysicsManager.Instance.fixedDeltaTime), properties.position.y);
			}
		}

		//At the end of a frame, moves us the appropriate amount on the Y axis
		private void ApplyVerticalVelocity()
		{
			if(!freezeMovementY && !isYMovementFrozenForSingleFrame)
			{
				properties.position = new Vector2(properties.position.x, properties.position.y + ((properties.velocity.y + properties.externalVelocity.y) * PhysicsManager.Instance.fixedDeltaTime));
			}
		}

		//Used internally for figuring out collisions
		private Rect GetRectAtPosition(Vector3 position)
		{
			if(boxCol != null)
			{
				float scaleMultiplierX = (boxCol.transform.localScale.x <= 0.0f) ? -1.0f : 1.0f;
				float scaleMultiplierY = (boxCol.transform.localScale.y <= 0.0f) ? -1.0f : 1.0f;
				return new Rect(position.x + (boxCol.offset.x * scaleMultiplierX) - boxCol.size.x / 2, position.y + (boxCol.offset.y * scaleMultiplierY) - boxCol.size.y / 2, boxCol.size.x, boxCol.size.y);
			}
			else
			{
				return new Rect(0, 0, 1, 1);
			}
		}

		#endregion

		//Registers this object with the PhysicsManager, which will allow it to move every frame
		void OnEnable()
		{
			PhysicsManager.Instance.RegisterPhysicsObject(this);
			properties.position = transform.position;
			previousFrameProperties.position = transform.position;
			SyncGravityScale();
		}

		void OnDisable()
		{
			PhysicsManager.Instance.UnregisterPhysicsObject(this);
		}
	}

}
