/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastHelper:MonoBehaviour 
{
	public class LedgeInfo
	{
		public bool didHit;
		public float hitY;
	}

	void Awake() 
	{
		
	}

	public static bool IsNextToLedge(Direction.Horizontal _direction, Direction.Vertical _verticalDirection, BoxCollider2D _collider, float gravityScaleMultiplier = 1.0f)
	{
		RaycastHit2D raycastHits = new RaycastHit2D();
		int collisionLayerMask = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("PassThroughBottom");

		float rayLength = _collider.size.y * 0.5f + 0.1f;
		Vector2 rayDirection = (_verticalDirection == Direction.Vertical.Up) ? Vector2.up : Vector2.down;
		bool isConnected = false;

		Vector2 rayOrigin = new Vector2(_collider.transform.position.x, _collider.transform.position.y + (_collider.offset.y * gravityScaleMultiplier));

		float raycastSpacing = _collider.size.x * 0.5f;
		rayOrigin.x = (_direction == Direction.Horizontal.Left) ? rayOrigin.x - raycastSpacing : rayOrigin.x + raycastSpacing;

		raycastHits = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMask);
		if(raycastHits.fraction > 0) 
		{
			isConnected = true;
		}

		#if UNITY_EDITOR
		//Debug.DrawRay(rayOrigin, new Vector2(0.0f, (int)_verticalDirection) * rayLength, Color.red);
		#endif

		return !isConnected;
	}

	public static LedgeInfo DetectLedgeOnWall(Direction.Horizontal _direction, Direction.Vertical _verticalDirection, BoxCollider2D _collider, float velocityToCheck, float offset = 0.25f)
	{
		RaycastHit2D raycastHits = new RaycastHit2D();
		int collisionLayerMask = 1 << LayerMask.NameToLayer("Terrain");

		float rayLength = _collider.size.y + Mathf.Abs(velocityToCheck) + offset;
		Vector2 rayDirection = (_verticalDirection == Direction.Vertical.Up) ? Vector2.up : Vector2.down;
		bool isConnected = false;

		Vector2 rayOrigin = new Vector2(_collider.transform.position.x + (_collider.size.x * 0.5f) * (int)_direction, _collider.transform.position.y - (_collider.size.y * 0.5f * (int)_verticalDirection));
		rayOrigin.y = (rayDirection == Vector2.up) ? _collider.bounds.min.y - offset : _collider.bounds.max.y + offset;

		float raycastSpacing = _collider.size.x * 0.5f;
		rayOrigin.x = (_direction == Direction.Horizontal.Left) ? rayOrigin.x - raycastSpacing : rayOrigin.x + raycastSpacing;

		raycastHits = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMask);
		if(raycastHits.fraction > 0) 
		{
			isConnected = true;
		}

		#if UNITY_EDITOR
		//Debug.DrawRay(rayOrigin, new Vector2(0.25f * (int)_direction, (int)rayDirection.y * rayLength), Color.red);
		#endif

		float hitY = (isConnected) ? raycastHits.point.y : 0.0f;
		LedgeInfo ledgeInfo = new LedgeInfo();
		ledgeInfo.didHit = isConnected;
		ledgeInfo.hitY = hitY;

		return ledgeInfo;
	}

	public static bool IsOnSurface(string surfaceTag, Direction.Vertical _verticalDirection, BoxCollider2D _collider, float gravityScaleMultiplier = 1.0f)
	{
		RaycastHit2D raycastHits = new RaycastHit2D();
		int collisionLayerMask = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("PassThroughBottom");

		float rayLength = _collider.size.y * 0.5f + 0.25f;
		Vector2 rayDirection = (_verticalDirection == Direction.Vertical.Up) ? Vector2.up : Vector2.down;
		bool isConnected = false;

		Vector2 rayOrigin = new Vector2(_collider.transform.position.x, _collider.transform.position.y + (_collider.offset.y * gravityScaleMultiplier));

		float raycastSpacing = _collider.size.x * 0.5f;
		rayOrigin.x = rayOrigin.x - raycastSpacing;

		for(int i = 0; i < 3; i ++)
		{
			raycastHits = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMask);
			if(raycastHits.fraction > 0) 
			{
				if(raycastHits.collider.tag == surfaceTag)
				isConnected = true;
			}

			#if UNITY_EDITOR
			//Debug.DrawRay(rayOrigin, new Vector2(0.0f, (int)_verticalDirection) * rayLength, Color.red);
			#endif

			rayOrigin.x += raycastSpacing;
		}

		return isConnected;
	}

	public static bool IsUnderOverhang(Direction.Vertical _verticalDirection, Vector2 _colliderSize, Vector3 _colliderPosition)
	{
		RaycastHit2D raycastHits = new RaycastHit2D();
		int collisionLayerMask = 1 << LayerMask.NameToLayer("Terrain");
		float sideBuffer = 0.025f; //This pulls the raycasts slightly in from the sides of the collider; it prevents the player from crouching NEXT TO an overhang and being unable to get back up

		float rayLength = _colliderSize.y * 0.5f + 0.25f;
		Vector2 rayDirection = (_verticalDirection == Direction.Vertical.Up) ? Vector2.up : Vector2.down;
		bool isConnected = false;

		Vector2 rayOrigin = new Vector2(_colliderPosition.x, _colliderPosition.y);

		float raycastSpacing = _colliderSize.x * 0.5f - sideBuffer;
		rayOrigin.x = rayOrigin.x - raycastSpacing;

		for(int i = 0; i < 3; i ++)
		{
			raycastHits = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMask);
			if(raycastHits.fraction > 0) 
			{
				isConnected = true;
			}

			#if UNITY_EDITOR
			//Debug.DrawRay(rayOrigin, new Vector2(0.0f, (int)_verticalDirection) * rayLength, Color.red);
			#endif

			rayOrigin.x += raycastSpacing;
		}

		return isConnected;
	}

	public static Collider2D GetColliderForSurface(string surfaceTag, Direction.Vertical _verticalDirection, BoxCollider2D _collider, float gravityScaleMultiplier = 1.0f)
	{
		RaycastHit2D raycastHits = new RaycastHit2D();
		int collisionLayerMask = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("PassThroughBottom");

		float rayLength = _collider.size.y * 0.5f + 0.25f;
		Vector2 rayDirection = (_verticalDirection == Direction.Vertical.Up) ? Vector2.up : Vector2.down;
		bool isConnected = false;

		Vector2 rayOrigin = new Vector2(_collider.transform.position.x, _collider.transform.position.y + (_collider.offset.y * gravityScaleMultiplier));

		float raycastSpacing = _collider.size.x * 0.5f;
		rayOrigin.x = rayOrigin.x - raycastSpacing;

		for(int i = 0; i < 3; i ++)
		{
			raycastHits = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMask);
			if(raycastHits.fraction > 0) 
			{
				if(raycastHits.collider.tag == surfaceTag)
				{
					isConnected = true;
					return raycastHits.collider;
				}
			}

			#if UNITY_EDITOR
			//Debug.DrawRay(rayOrigin, new Vector2(0.0f, (int)_verticalDirection) * rayLength, Color.red);
			#endif

			rayOrigin.x += raycastSpacing;
		}

		return null;
	}

	public static bool DropThroughFloorRaycast(Direction.Vertical _verticalDirection, BoxCollider2D _collider, float gravityScaleMultiplier = 1.0f)
	{
		RaycastHit2D raycastHits = new RaycastHit2D();
		int collisionLayerMask = 1 << LayerMask.NameToLayer("PassThroughBottom");

		Vector3 rayDirection = (_verticalDirection == Direction.Vertical.Down) ? Vector3.down : Vector3.up;
		bool isConnected = false;

		Vector2 rayOrigin = new Vector2(_collider.transform.position.x, _collider.transform.position.y + (_collider.offset.y * gravityScaleMultiplier));
		float raycastSpacing = _collider.size.x * 0.5f;
		float rayLength = _collider.size.y * 0.5f + 0.05f;
		rayOrigin.x -= rayLength;

		for(int i = 0; i < 3; i ++)
		{
			raycastHits = Physics2D.Raycast(rayOrigin, rayDirection, rayLength, collisionLayerMask);
			if(raycastHits.fraction > 0) 
			{
				isConnected = true;
			}

			//Debug.DrawRay(rayOrigin, rayDirection * rayLength, Color.red);

			rayOrigin.x += rayLength;
		}

		return isConnected;
	}
}
