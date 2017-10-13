/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;

namespace RexEngine
{
	public class RexCamera:MonoBehaviour 
	{
		public Camera camera;
		public RexActor focusObject; //The object the camera will focus on and follow
		public Camera foregroundCamera;
		public Camera midgroundCamera;
		public Camera backgroundCamera;
		public Camera canvasCamera;
		public Camera uiCamera;
		public bool willTrackFocusObject;
		public bool willScrollHorizontally = true;
		public bool willScrollVertically = true;

		[System.Serializable]
		public class ScrollProperties
		{
			[HideInInspector]
			public bool willScrollForegroundX = true;

			[HideInInspector]
			public bool willScrollForegroundY = true;

			[HideInInspector]
			public bool willScrollMidgroundX = true;

			[HideInInspector]
			public bool willScrollMidgroundY = true;

			[HideInInspector]
			public bool willScrollBackgroundX = true;

			[HideInInspector]
			public bool willScrollBackgroundY = true;
		}

		//The below willScroll properties are set by a LevelManager automatically on its Awake based on the settings you give it in the Inspector
		public ScrollProperties scrolling;

		protected Vector2 offsetFromFocusObject;

		[System.NonSerialized]
		public Vector2 shakeOffset; //The offset that ScreenShake is giving the camera; should not be set directly

		[HideInInspector]
		public Vector3 rawPosition; //Camera position before external factors like Shake

		[HideInInspector]
		public Vector2 boundariesMin; //Set by objects like the Boundary object

		[HideInInspector]
		public Vector2 boundariesMax; //Set by objects like the Boundary object

		void Awake() 
		{
			rawPosition = transform.position;

			if(canvasCamera)
			{
				canvasCamera.cullingMask = 1 << LayerMask.NameToLayer("Canvas");
			}

			if(backgroundCamera)
			{
				backgroundCamera.cullingMask = 1 << LayerMask.NameToLayer("Background");
			}

			if(midgroundCamera)
			{
				midgroundCamera.cullingMask = 1 << LayerMask.NameToLayer("Midground");
			}

			if(foregroundCamera)
			{
				foregroundCamera.cullingMask = 1 << LayerMask.NameToLayer("Foreground");
			}

			if(uiCamera)
			{
				uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
			}

			camera.cullingMask = 1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("TransparentFX") | 1 << LayerMask.NameToLayer("Ignore Raycast") | 1 << LayerMask.NameToLayer("Water") | 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("PassThroughBottom");
		}

		void Start() 
		{
			ScreenShake.Instance.SetCamera(this);
		}

		void LateUpdate() 
		{
			if(Time.timeScale > 0)
			{
				UpdateCameras();
			}

			//TODO: New

			if(Input.GetKeyDown(KeyCode.Q))
			{
				if(midgroundCamera.gameObject.activeSelf)
				{
					midgroundCamera.gameObject.SetActive(false);
					backgroundCamera.gameObject.SetActive(false);
					foregroundCamera.gameObject.SetActive(false);
				}
				else
				{
					midgroundCamera.gameObject.SetActive(true);
					backgroundCamera.gameObject.SetActive(true);
					foregroundCamera.gameObject.SetActive(true);
				}
			}

			//TODO: End new
		}

		public void SetPosition(Vector2 position)
		{
			rawPosition = new Vector3(position.x, position.y, rawPosition.z);
			transform.position = rawPosition;

			UpdateCameras();
		}

		public void SetFocusObject(RexActor _focusObject)
		{
			focusObject = _focusObject;
		}

		protected void UpdateCameras()
		{
			Vector3 newPosition;

			newPosition = FocusOnFocusObject(rawPosition);
			newPosition = SnapToCameraBoundaries(newPosition);
			newPosition = StopDirectionalScrollingIfDisabled(newPosition);

			rawPosition = newPosition;

			newPosition = ApplyShake(newPosition);

			transform.position = newPosition;

			ScrollSecondaryCameras();
		}

		protected Vector3 LerpToNewPosition(Vector3 position)
		{
			Vector2 maxChange = new Vector2(0.75f, 1.5f);

			Vector3 adjustedPosition = position;
			if(Mathf.Abs(position.x - transform.position.x) > maxChange.x)
			{
				if(adjustedPosition.x > transform.position.x)
				{
					adjustedPosition.x = transform.position.x + maxChange.x;
				}
				else if(adjustedPosition.x < transform.position.x)
				{
					adjustedPosition.x = transform.position.x - maxChange.x;
				}
			}

			if(Mathf.Abs(position.y - transform.position.y) > maxChange.y)
			{
				if(adjustedPosition.y > transform.position.y)
				{
					adjustedPosition.y = transform.position.y + maxChange.y;
				}
				else if(adjustedPosition.y < transform.position.y)
				{
					adjustedPosition.y = transform.position.y - maxChange.y;
				}
			}

			return adjustedPosition;
		}

		protected Vector3 FocusOnFocusObject(Vector3 position)
		{
			Vector3 adjustedPosition = position;
			if(willTrackFocusObject && focusObject)
			{
				adjustedPosition = new Vector3(focusObject.transform.position.x + offsetFromFocusObject.x, focusObject.transform.position.y + offsetFromFocusObject.y, transform.position.z);
			}

			return adjustedPosition;
		}

		protected Vector3 ApplyShake(Vector3 position)
		{
			Vector3 positionWithShake = new Vector3(position.x + shakeOffset.x, position.y + shakeOffset.y, transform.position.z);
			return positionWithShake;
		}

		protected Vector3 SnapToCameraBoundaries(Vector3 position)
		{
			if(position.y > boundariesMax.y)
			{
				position.y = boundariesMax.y;
			}

			if(position.y < boundariesMin.y)
			{
				position.y = boundariesMin.y;
			}

			if(position.x > boundariesMax.x)
			{
				position.x = boundariesMax.x;
			}

			if(position.x < boundariesMin.x)
			{
				position.x = boundariesMin.x;
			}

			return position;
		}

		protected Vector3 StopDirectionalScrollingIfDisabled(Vector3 position)
		{
			Vector3 stoppedPosition = position;
			if(!willScrollHorizontally)
			{
				stoppedPosition.x = transform.position.x - shakeOffset.x;
			}
			if(!willScrollVertically)
			{
				stoppedPosition.y = transform.position.y - shakeOffset.y;
			}

			return stoppedPosition;
		}

		private void ScrollSecondaryCameras()
		{
			float backgroundOffset = 0.35f;
			float midgroundOffset = 0.5f;
			float foregroundOffset = 1.2f; 

			if(foregroundCamera != null)
			{
				float foregroundX = (scrolling.willScrollForegroundX) ? transform.position.x * foregroundOffset : transform.position.x;
				float foregroundY = (scrolling.willScrollForegroundY) ? transform.position.y * foregroundOffset : transform.position.y;
				foregroundCamera.transform.position = new Vector3(foregroundX, foregroundY, foregroundCamera.transform.position.z);
			}

			if(midgroundCamera != null)
			{
				float midgroundX = (scrolling.willScrollMidgroundX) ? transform.position.x * midgroundOffset : transform.position.x;
				float midgroundY = (scrolling.willScrollMidgroundY) ? transform.position.y * midgroundOffset : transform.position.y;
				midgroundCamera.transform.position = new Vector3(midgroundX, midgroundY, midgroundCamera.transform.position.z);
			}

			if(backgroundCamera != null)
			{
				float backgroundX = (scrolling.willScrollBackgroundX) ? transform.position.x * backgroundOffset : transform.position.x;
				float backgroundY = (scrolling.willScrollBackgroundY) ? transform.position.y * backgroundOffset : transform.position.y;
				backgroundCamera.transform.position = new Vector3(backgroundX, backgroundY, backgroundCamera.transform.position.z);
			}
		}

		void OnDestroy()
		{
			camera = null;
			focusObject = null;	
		}
	}
}
