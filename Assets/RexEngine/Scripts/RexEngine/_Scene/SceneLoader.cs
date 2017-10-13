/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RexEngine
{
	public class SceneLoader:MonoBehaviour 
	{
		public string identifier = "A";
		public string levelToLoad = "Level_1";
		public string loadPoint = "A";
		public SceneBoundary.Edge edge;
		public SceneLoadType sceneLoadType;
		public int tiles = 5;
		public bool willSnapToSceneEdges = true;
		public bool isAttachedToGameObject = false;

		private bool hasTriggeredLoad;

		public enum SceneLoadType
		{
			ExitAndEntrance,
			EntranceOnly
		}

		void Awake()
		{
			RegisterWithRexSceneManager();
			if(sceneLoadType == SceneLoadType.EntranceOnly)
			{
				GetComponent<Collider2D>().isTrigger = false;
				gameObject.layer = LayerMask.NameToLayer("Terrain");
			}
		}

		void Start()
		{
			if(sceneLoadType == SceneLoadType.EntranceOnly) //The "Boundaries" layer is for actors tagged as "Player" only; it prevents them from walking off the side of a scene, while allowing enemies or projectiles to freely exit
			{
				gameObject.layer = LayerMask.NameToLayer("Boundaries");
			}
		}

		void OnTriggerEnter2D(Collider2D col) 
		{
			if(isAttachedToGameObject)
			{
				return;
			}

			if(col.tag == "Player" && sceneLoadType != SceneLoadType.EntranceOnly && col.GetComponent<RexActor>())
			{	
				if(!col.GetComponent<RexActor>().isBeingLoadedIntoNewScene)
				{
					if(edge == SceneBoundary.Edge.Left || edge == SceneBoundary.Edge.Right)
					{
						RexSceneManager.Instance.playerOffsetFromSceneLoader = col.transform.position.y - transform.position.y;
					}
					else if(edge == SceneBoundary.Edge.Top || edge == SceneBoundary.Edge.Bottom)
					{
						RexSceneManager.Instance.playerOffsetFromSceneLoader = col.transform.position.x - transform.position.x;
					}

					LoadSceneWithFadeOut();
				}
			}
		}

		public void LoadSceneWithFadeOut()
		{
			if(!hasTriggeredLoad)
			{
				GameManager.Instance.player.GetComponent<RexActor>().isBeingLoadedIntoNewScene = true;
				hasTriggeredLoad = true;
				RexSceneManager.Instance.playerSpawnType = RexSceneManager.PlayerSpawnType.SceneLoader;
				RexSceneManager.Instance.loadPoint = loadPoint;
				RexSceneManager.Instance.LoadSceneWithFadeOut(levelToLoad, Color.black);
			}
		}

		private void RegisterWithRexSceneManager()
		{
			RexSceneManager.Instance.RegisterSceneLoader(this);
		}

		private float GetDistanceToTerrain(Vector2 rayDirection)
		{
			float distanceToTerrain = 0.0f;
			RaycastHit2D hitInfo = new RaycastHit2D();
			int collisionLayerMask = 1 << LayerMask.NameToLayer("Terrain");

			float sideRayLength = 1000.0f;
			bool connected = false;

			Vector2 origin = new Vector2(transform.position.x, transform.position.y);

			hitInfo = Physics2D.Raycast(origin, rayDirection, sideRayLength, collisionLayerMask);
			if(hitInfo.fraction > 0) 
			{
				connected = true;
				distanceToTerrain = hitInfo.distance;
			}

			Debug.DrawRay(origin, rayDirection * sideRayLength, Color.red);

			return distanceToTerrain;
		}

		public void SnapToSceneBoundaries()
		{
			float distanceFromCameraSide = 0.5f;
			BoxCollider2D col = GetComponent<BoxCollider2D>();

			if(edge == SceneBoundary.Edge.Right)
			{
				float xPosition = transform.position.x;
				if(GameObject.Find("SceneBoundary_right") != null)
				{
					xPosition = GameObject.Find("SceneBoundary_right").transform.position.x + distanceFromCameraSide;
				}

				transform.position = new Vector3(xPosition + col.size.x / 2.0f, transform.position.y, 0.0f);
			}
			else if(edge == SceneBoundary.Edge.Left)
			{
				float xPosition = transform.position.x;
				if(GameObject.Find("SceneBoundary_left") != null)
				{
					xPosition = GameObject.Find("SceneBoundary_left").transform.position.x - distanceFromCameraSide;
				}

				transform.position = new Vector3(xPosition - col.size.x / 2.0f, transform.position.y, 0.0f);
			}
			else if(edge == SceneBoundary.Edge.Top)
			{
				float yPosition = transform.position.y;
				if(GameObject.Find("SceneBoundary_top") != null)
				{
					yPosition = GameObject.Find("SceneBoundary_top").transform.position.y + distanceFromCameraSide;
				}

				transform.position = new Vector3(transform.position.x, yPosition + col.size.y / 2.0f, 0.0f);
			}
			else if(edge == SceneBoundary.Edge.Bottom)
			{
				float yPosition = transform.position.y;
				if(GameObject.Find("SceneBoundary_bottom") != null)
				{
					yPosition = GameObject.Find("SceneBoundary_bottom").transform.position.y - distanceFromCameraSide;
				}

				transform.position = new Vector3(transform.position.x, yPosition - col.size.y / 2.0f, 0.0f);
			}
		}

		protected void PositionOnGrid()
		{
			if(tiles < 1)
			{
				tiles = 1;
			}

			float distanceFromCameraSide = GlobalValues.tileSize * 0.5f;
			float colliderWidth = GlobalValues.tileSize * 5.0f;

			BoxCollider2D col = GetComponent<BoxCollider2D>();
			if(edge == SceneBoundary.Edge.Left || edge == SceneBoundary.Edge.Right)
			{
				col.size = new Vector2(colliderWidth, tiles * GlobalValues.tileSize);

				float newPosition = Mathf.CeilToInt((transform.position.y - (GlobalValues.tileSize * 0.5f)) / GlobalValues.tileSize);
				transform.position = new Vector3(transform.position.x, (newPosition * GlobalValues.tileSize) + (GlobalValues.tileSize * 0.5f), transform.position.z);
			}
			else if(edge == SceneBoundary.Edge.Top || edge == SceneBoundary.Edge.Bottom)
			{
				col.size = new Vector2(tiles * GlobalValues.tileSize, colliderWidth);

				float newPosition = Mathf.CeilToInt((transform.position.x - (GlobalValues.tileSize * 0.5f)) / GlobalValues.tileSize);
				transform.position = new Vector3((newPosition * GlobalValues.tileSize) + (GlobalValues.tileSize * 0.5f), transform.position.y, transform.position.z);
			}
		}

		#if UNITY_EDITOR
		void OnDrawGizmos() 
		{
			if(isAttachedToGameObject)
			{
				return;
			}

			Vector3 position = transform.position;
			Vector3 startPoint = new Vector3();
			Vector3 endPoint = new Vector3();
			GUIStyle style = new GUIStyle();
			string iconName = "";
			Texture2D texture;

			PositionOnGrid();

			if(willSnapToSceneEdges)
			{
				SnapToSceneBoundaries();
			}

			float zoom = SceneView.currentDrawingSceneView.camera.orthographicSize;
			float zoomAdjustment = zoom / 20;

			BoxCollider2D collider = GetComponent<BoxCollider2D>();

			GUIStyle iconStyle = new GUIStyle();
			iconStyle.alignment = TextAnchor.MiddleCenter;

			switch(edge)
			{
				case SceneBoundary.Edge.Left:
					position.x = position.x + GetComponent<Collider2D>().bounds.size.x / 2.0f;
					startPoint = new Vector3(position.x, position.y - GetComponent<Collider2D>().bounds.size.y / 2.0f, 0.0f);
					endPoint = new Vector3(position.x, position.y + GetComponent<Collider2D>().bounds.size.y / 2.0f, 0.0f);
					style.alignment = TextAnchor.MiddleLeft;

					Handles.color = new Color(1.0f, 0.25f, 0.25f, 1.0f);
					Handles.DrawDottedLine(startPoint, endPoint, 2.0f);

					style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

					iconStyle.alignment = TextAnchor.MiddleLeft;
					iconStyle.contentOffset = new Vector2(-222.0f, -18.0f);
					texture = AssetDatabase.LoadAssetAtPath("Assets/RexEngine/Gizmos/SceneLoaderLeft.png", typeof(Texture2D)) as Texture2D;
					Handles.Label(position, new GUIContent(texture), iconStyle);

					style.contentOffset = new Vector2(-6.0f, -9.0f);
					style.fontStyle = FontStyle.Bold;
					style.fontSize = 20;
					Handles.Label(position, identifier, style);

					style.contentOffset = new Vector2(-150.0f, -6.0f);
					style.fontStyle = FontStyle.Normal;
					style.fontSize = 12;
					Handles.Label(position, levelToLoad, style);

					style.contentOffset = new Vector2(-207.0f, -6.0f);
					Handles.Label(position, loadPoint, style);

					break;
				case SceneBoundary.Edge.Right:
					position.x = position.x - collider.bounds.size.x / 2.0f;
					startPoint = new Vector3(position.x, position.y - collider.bounds.size.y / 2.0f, 0.0f);
					endPoint = new Vector3(position.x, position.y + collider.bounds.size.y / 2.0f, 0.0f);
					style.alignment = TextAnchor.MiddleLeft;

					Handles.color = new Color(1.0f, 0.25f, 0.25f, 1.0f);
					Handles.DrawDottedLine(startPoint, endPoint, 2.0f);

					style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

					iconStyle.alignment = TextAnchor.MiddleRight;
					iconStyle.contentOffset = new Vector2(-28.0f, -18.0f);
					texture = AssetDatabase.LoadAssetAtPath("Assets/RexEngine/Gizmos/SceneLoaderRight.png", typeof(Texture2D)) as Texture2D;
					Handles.Label(position, new GUIContent(texture), iconStyle);

					style.contentOffset = new Vector2(-4.0f, -9.0f);
					style.fontStyle = FontStyle.Bold;
					style.fontSize = 20;
					Handles.Label(position, identifier, style);

					style.contentOffset = new Vector2(40.0f, -6.0f);
					style.fontStyle = FontStyle.Normal;
					style.fontSize = 12;
					Handles.Label(position, levelToLoad, style);

					style.contentOffset = new Vector2(205.0f, -6.0f);
					Handles.Label(position, loadPoint, style);

					break;
				case SceneBoundary.Edge.Top:
					position.y = position.y -collider.bounds.size.y / 2.0f;
					startPoint = new Vector3(position.x - collider.bounds.size.x / 2.0f, position.y, 0.0f);
					endPoint = new Vector3(position.x + collider.bounds.size.x / 2.0f, position.y, 0.0f);
					style.alignment = TextAnchor.MiddleCenter;

					Handles.color = new Color(1.0f, 0.25f, 0.25f, 1.0f);
					Handles.DrawDottedLine(startPoint, endPoint, 2.0f);

					style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

					iconStyle.alignment = TextAnchor.LowerCenter;
					iconStyle.contentOffset = new Vector2(-47.5f, -101.0f);
					texture = AssetDatabase.LoadAssetAtPath("Assets/RexEngine/Gizmos/SceneLoaderTop.png", typeof(Texture2D)) as Texture2D;
					Handles.Label(position, new GUIContent(texture), iconStyle);

					style.fixedWidth = collider.size.x;
					style.contentOffset = new Vector2(1.0f, -8.0f);
					style.fontStyle = FontStyle.Bold;
					style.fontSize = 20;
					Handles.Label(position, identifier, style);

					style.contentOffset = new Vector2(2.0f, -45.0f);
					style.fontStyle = FontStyle.Normal;
					style.fontSize = 12;
					Handles.Label(position, levelToLoad, style);

					style.contentOffset = new Vector2(2.0f, -90.0f);
					Handles.Label(position, loadPoint, style);

					break;
				case SceneBoundary.Edge.Bottom:
					position.y = position.y + collider.bounds.size.y / 2.0f;
					startPoint = new Vector3(position.x - collider.bounds.size.x / 2.0f, position.y, 0.0f);
					endPoint = new Vector3(position.x + collider.bounds.size.x / 2.0f, position.y, 0.0f);
					style.alignment = TextAnchor.MiddleCenter;

					Handles.color = new Color(1.0f, 0.25f, 0.25f, 1.0f);
					Handles.DrawDottedLine(startPoint, endPoint, 2.0f);

					style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

					iconStyle.alignment = TextAnchor.UpperCenter;
					iconStyle.contentOffset = new Vector2(-47.5f, -28.0f);
					texture = AssetDatabase.LoadAssetAtPath("Assets/RexEngine/Gizmos/SceneLoaderBottom.png", typeof(Texture2D)) as Texture2D;
					Handles.Label(position, new GUIContent(texture), iconStyle);

					style.fixedWidth = collider.size.x;
					style.contentOffset = new Vector2(1.0f, -8.0f);
					style.fontStyle = FontStyle.Bold;
					style.fontSize = 20;
					Handles.Label(position, identifier, style);

					style.contentOffset = new Vector2(1.0f, 35.0f);
					style.fontStyle = FontStyle.Normal;
					style.fontSize = 12;
					Handles.Label(position, levelToLoad, style);

					style.contentOffset = new Vector2(1.0f, 78.0f);
					Handles.Label(position, loadPoint, style);

					break;
			}
		}
		#endif
	}

}
