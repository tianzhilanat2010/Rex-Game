/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace RexEngine
{
	[SelectionBase]
	public class SceneBoundary:MonoBehaviour
	{
		public enum Edge
		{
			Left,
			Right,
			Bottom,
			Top,
			None
		}

		[HideInInspector]
		public Edge edge;

		[HideInInspector]
		public int gridIndex;

		public bool isBottomlessPit; //If set to True, this will kill the player when they enter it
		public bool isSolid; //If set to True, the player can't walk through this

		private float gizmoLineLength = 10000.0f;
		private Color gizmoColorAwake = new Color(1.0f, 0.8f, 0.5f, 1.0f);
		private Color gizmoColorSide = new Color(1.0f, 0.8f, 0.5f, 0.25f);
		private Color gizmoColorCollide = new Color(0.5f, 0.8f, 1.0f, 1.0f);

		void Start() 
		{
			SetSceneBoundary();
			PositionCollider();

			if(isSolid)
			{
				gameObject.layer = LayerMask.NameToLayer("Boundaries");
			}
		}

		public void SetPosition(Vector2 position)
		{
			transform.position = new Vector3(position.x, position.y, 0.0f);
			SetSceneBoundary();
		}

		public void SetSceneBoundary()
		{
			RexCamera camera = Camera.main.GetComponent<RexCamera>();
			if(camera != null)
			{
				if(edge == Edge.Left)
				{
					camera.boundariesMin.x = transform.position.x + CameraHelper.GetScreenSizeInUnits().x * 0.5f;
				}
				else if(edge == Edge.Right)
				{
					camera.boundariesMax.x = transform.position.x - CameraHelper.GetScreenSizeInUnits().x * 0.5f;
				}
				else if(edge == Edge.Bottom)
				{
					camera.boundariesMin.y = transform.position.y + CameraHelper.GetScreenSizeInUnits().y * 0.5f;
				}
				else if(edge == Edge.Top)
				{
					camera.boundariesMax.y = transform.position.y - CameraHelper.GetScreenSizeInUnits().y * 0.5f;
				}
			}
		}

		void OnDrawGizmos() 
		{
			#if UNITY_EDITOR
			Handles.color = gizmoColorAwake;

			Vector3 startPoint = new Vector3(0.0f, 0.0f, 1.1f);
			Vector3 endPoint = new Vector3(0.0f, 0.0f, 1.1f);
			Vector2 sideLineOffset = new Vector2(0, 0);
			float sideLineOffsetSize = 0.05f;

			if(edge == Edge.Top || edge == Edge.Bottom)
			{
				startPoint.x = -gizmoLineLength;
				endPoint.x = gizmoLineLength;
				startPoint.y = endPoint.y = transform.position.y;

				if(edge == Edge.Top)
				{
					sideLineOffset.y = sideLineOffsetSize;
				}
				else
				{
					sideLineOffset.y = -sideLineOffsetSize;
				}
			}
			else if(edge == Edge.Left || edge == Edge.Right)
			{
				startPoint.x = endPoint.x = transform.position.x;
				startPoint.y = gizmoLineLength;
				endPoint.y = -gizmoLineLength;

				if(edge == Edge.Left)
				{
					sideLineOffset.x = -sideLineOffsetSize;
				}
				else
				{
					sideLineOffset.x = sideLineOffsetSize;
				}
			}
		
			Handles.DrawLine(startPoint, endPoint);

			Handles.color = gizmoColorSide;
			for(int i = 0; i < 10; i ++)
			{
				startPoint.x += sideLineOffset.x;
				endPoint.x += sideLineOffset.x;

				startPoint.y += sideLineOffset.y;
				endPoint.y += sideLineOffset.y;

				Handles.DrawLine(startPoint, endPoint);
			}

			#endif

			PositionCollider();

			#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				SnapToRoomSize();
				GUIStyle style = new GUIStyle();
				style.normal.textColor = gizmoColorAwake;
				string text = gridIndex.ToString();
				Vector3 position = transform.position;
				float margin = 1.0f;
				string newName = "SceneBoundary_";
				switch(edge)
				{
					case Edge.Left:
						position.x = transform.position.x - margin * 5.0f;
						newName += "left";
						break;
					case Edge.Right:
						position.x = transform.position.x + margin * 2.0f;
						newName += "right";
						break;
					case Edge.Bottom:
						position.y = transform.position.y;
						newName += "bottom";
						break;
					case Edge.Top:
						position.y = transform.position.y + margin * 5.0f;
						newName += "top";
						break;
				}
				Handles.Label(position, text, style);

				string gameObjectName = gameObject.name.Split('_')[0];
				if(gameObjectName == "CameraBoundary")
				{
					gameObject.name = newName;
					EditorUtility.SetDirty(gameObject);
					EditorSceneManager.MarkSceneDirty(gameObject.scene);
				}

			}
			#endif
		}

		protected void SnapToRoomSize()
		{
			int roomPosition = 0;
			if(edge == Edge.Right)
			{
				roomPosition = Mathf.CeilToInt((transform.position.x - (GlobalValues.roomSize.x * 0.5f)) / GlobalValues.roomSize.x);
				if(roomPosition < 0)
				{
					roomPosition = 0;
				}

				int adjusted = roomPosition + 1;
				transform.position = new Vector3((roomPosition * GlobalValues.roomSize.x) + (GlobalValues.roomSize.x * 0.5f), transform.position.y, transform.position.z);
			}
			else if(edge == Edge.Left)
			{
				roomPosition = Mathf.FloorToInt((transform.position.x + (GlobalValues.roomSize.x * 0.5f)) / GlobalValues.roomSize.x);
				if(roomPosition > 0)
				{
					roomPosition = 0;
				}
				transform.position = new Vector3((roomPosition * GlobalValues.roomSize.x) - (GlobalValues.roomSize.x * 0.5f), transform.position.y, transform.position.z);
			}
			else if(edge == Edge.Top)
			{
				roomPosition = Mathf.FloorToInt((transform.position.y - (GlobalValues.roomSize.y * 0.5f)) / GlobalValues.roomSize.y);
				if(roomPosition < 0)
				{
					roomPosition = 0;
				}
				transform.position = new Vector3(transform.position.x, (roomPosition * GlobalValues.roomSize.y) + (GlobalValues.roomSize.y * 0.5f), transform.position.z);
			}
			else if(edge == Edge.Bottom)
			{
				roomPosition = Mathf.CeilToInt((transform.position.y + (GlobalValues.roomSize.y * 0.5f)) / GlobalValues.roomSize.y);
				if(roomPosition > 0)
				{
					roomPosition = 0;
				}
				transform.position = new Vector3(transform.position.x, (roomPosition * GlobalValues.roomSize.y) - (GlobalValues.roomSize.y * 0.5f), transform.position.z);
			}

			gridIndex = roomPosition;

			if(edge == Edge.Top || edge == Edge.Right)
			{
				gridIndex += 1; //This makes sure the grid index displays properly in gizmos
			}
		}

		private void PositionCollider()
		{
			float sideOffset = 5.0f * GlobalValues.tileSize;
			float size = 10.0f * GlobalValues.tileSize;

			if(edge == Edge.Right)
			{
				if(GameObject.Find("SceneBoundary_top") && GameObject.Find("SceneBoundary_bottom") && GetComponent<BoxCollider2D>() != null)
				{
					GetComponent<BoxCollider2D>().size = new Vector2(size, Mathf.Abs(GameObject.Find("SceneBoundary_top").transform.position.y - GameObject.Find("SceneBoundary_bottom").transform.position.y) + size * 2.0f);
					GetComponent<BoxCollider2D>().offset = new Vector2(sideOffset, GameObject.Find("SceneBoundary_top").transform.position.y - GetComponent<BoxCollider2D>().size.y / 2.0f + size);
					transform.position = new Vector3(transform.position.x, 0.0f, 0.0f);
				}
			}
			else if(edge == Edge.Left)
			{
				if(GameObject.Find("SceneBoundary_top") && GameObject.Find("SceneBoundary_bottom") && GetComponent<BoxCollider2D>() != null)
				{
					GetComponent<BoxCollider2D>().size = new Vector2(size, Mathf.Abs(GameObject.Find("SceneBoundary_top").transform.position.y - GameObject.Find("SceneBoundary_bottom").transform.position.y) + size * 2.0f);
					GetComponent<BoxCollider2D>().offset = new Vector2(-sideOffset, GameObject.Find("SceneBoundary_top").transform.position.y - GetComponent<BoxCollider2D>().size.y / 2.0f + size);
					transform.position = new Vector3(transform.position.x, 0.0f, 0.0f);
				}
			}
			else if(edge == Edge.Top)
			{
				if(GameObject.Find("SceneBoundary_left") && GameObject.Find("SceneBoundary_right") && GetComponent<BoxCollider2D>() != null)
				{
					GetComponent<BoxCollider2D>().size = new Vector2(Mathf.Abs(GameObject.Find("SceneBoundary_left").transform.position.x - GameObject.Find("SceneBoundary_right").transform.position.x) + size * 2.0f, size);
					GetComponent<BoxCollider2D>().offset = new Vector2(GameObject.Find("SceneBoundary_left").transform.position.x + GetComponent<BoxCollider2D>().size.x / 2.0f - size, sideOffset);
					transform.position = new Vector3(0.0f, transform.position.y, 0.0f);
				}
			}
			else if(edge == Edge.Bottom)
			{
				if(GameObject.Find("SceneBoundary_left") && GameObject.Find("SceneBoundary_right") && GetComponent<BoxCollider2D>() != null)
				{
					GetComponent<BoxCollider2D>().size = new Vector2(Mathf.Abs(GameObject.Find("SceneBoundary_left").transform.position.x - GameObject.Find("SceneBoundary_right").transform.position.x) + size * 2.0f, size);
					GetComponent<BoxCollider2D>().offset = new Vector2(GameObject.Find("SceneBoundary_left").transform.position.x + GetComponent<BoxCollider2D>().size.x / 2.0f - size, -sideOffset);
					transform.position = new Vector3(0.0f, transform.position.y, 0.0f);
				}
			}
		}

		private void ProcessCollision(Collider2D col)
		{
			RexObject rexObject = col.GetComponent<RexObject>();
			if(rexObject != null)
			{
				RexActor actor = col.GetComponent<RexActor>();
				if(actor != null && actor.isDead)
				{
					return;
				}

				if(rexObject.willDespawnOnSceneExit || isBottomlessPit)
				{
					float buffer = 0.5f * GlobalValues.tileSize;
					float warningBuffer = 0.15f * GlobalValues.tileSize;
					if(edge == Edge.Right)
					{
						if(rexObject.transform.position.x > transform.position.x + buffer)
						{
							rexObject.Clear();
						}
						else if(rexObject.transform.position.x > transform.position.x + warningBuffer)
						{
							rexObject.OnSceneBoundaryCollisionInsideBuffer();
						}
					}
					else if(edge == Edge.Left)
					{
						if(rexObject.transform.position.x < transform.position.x - buffer)
						{
							rexObject.Clear();
						}
						else if(rexObject.transform.position.x < transform.position.x - warningBuffer)
						{
							rexObject.OnSceneBoundaryCollisionInsideBuffer();
						}
					}
					else if(edge == Edge.Top)
					{
						if(rexObject.transform.position.y > transform.position.y + buffer)
						{
							rexObject.Clear();
						}
						else if(rexObject.transform.position.y > transform.position.y + warningBuffer)
						{
							rexObject.OnSceneBoundaryCollisionInsideBuffer();
						}
					}
					else if(edge == Edge.Bottom)
					{
						if(rexObject.transform.position.y < transform.position.y - buffer)
						{
							rexObject.Clear();

						}
						else if(rexObject.transform.position.y < transform.position.y - warningBuffer)
						{
							rexObject.OnSceneBoundaryCollisionInsideBuffer();
						}
					}
				}
			}
		}

		protected void CheckBottomlessPit(Collider2D col)
		{
			float bottomlessPitBuffer = 1.25f * GlobalValues.tileSize;
			if(isBottomlessPit)
			{
				if(GameManager.Instance.player != null && !GameManager.Instance.player.isBeingLoadedIntoNewScene && col.GetComponent<RexActor>() != null)
				{
					if(edge == Edge.Bottom)
					{
						if(col.transform.position.y < transform.position.y - bottomlessPitBuffer)
						{
							GameManager.Instance.OnPlayerEnteredBottomlessPit();
						}
						else
						{
							ReEnableCollider();
						}
					}
					else if(edge == Edge.Top)
					{
						if(col.transform.position.y > transform.position.y + bottomlessPitBuffer)
						{
							GameManager.Instance.OnPlayerEnteredBottomlessPit();
						}
						else
						{
							ReEnableCollider();
						}
					}
				}
			}
		}

		private void ReEnableCollider()
		{
			BoxCollider2D collider = GetComponent<BoxCollider2D>();
			if(collider != null)
			{
				collider.enabled = false;
				collider.enabled = true;
			}
		}

		private void OnTriggerEnter2D(Collider2D col)
		{
			if(col.tag == "Player" && col.GetComponent<RexActor>())
			{
				SetSceneBoundary();
				CheckBottomlessPit(col);
				if(col.transform.position.x > transform.position.x)
				{
					col.GetComponent<RexActor>().SetPosition(new Vector2(col.transform.position.x, col.transform.position.y));
				}
			}
			else if(col.tag != "Terrain" && col.tag != "Untagged")
			{
				ProcessCollision(col);
			}
		}
	}
}
