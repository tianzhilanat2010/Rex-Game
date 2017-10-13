/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class Setup:MonoBehaviour 
	{
		void Awake()
		{
			if(GameObject.Find("Singletons") == null)
			{
				GameObject prefab = Resources.Load("System/Singletons") as GameObject;
				GameObject newObject = Instantiate(prefab).gameObject;
				newObject.name = "Singletons";
				DontDestroyOnLoad(newObject);
			}

			if(GameObject.Find("UI") == null)
			{
				GameObject prefab = Resources.Load("System/UI") as GameObject;
				GameObject newObject = Instantiate(prefab).gameObject;
				newObject.name = "UI";
				DontDestroyOnLoad(newObject);
			}

			if(GameObject.Find("Player") == null && GameManager.Instance.player == null)
			{
				GameObject prefab = GameManager.Instance.playerPrefab.gameObject;
				GameObject newObject = Instantiate(prefab).gameObject;	
				newObject.name = newObject.name.Split('(')[0];
				DontDestroyOnLoad(newObject);

				GameManager.Instance.player = GameObject.FindGameObjectWithTag("Player").GetComponent<RexActor>();
				RexSceneManager.Instance.MovePlayerToSpawnPoint(); 

				newObject.layer = LayerMask.NameToLayer("Player");
				newObject.tag = "Player";
				foreach(Transform childObject in newObject.GetComponentsInChildren<Transform>())
				{
					if(childObject.GetComponent<RexController>() != null)
					{
						newObject.layer = LayerMask.NameToLayer("Default");
						childObject.tag = "Untagged";
					}
					else if(childObject.GetComponent<Attack>() == null)
					{
						childObject.gameObject.layer = LayerMask.NameToLayer("Player");
						childObject.tag = "Player";
					}
					else
					{
						if(childObject.tag != "Reflector")
						{
							childObject.gameObject.layer = LayerMask.NameToLayer("Default");
							childObject.tag = "Untagged";
						}
					}

					SpriteRenderer spriteRenderer = childObject.GetComponent<SpriteRenderer>();
					if(spriteRenderer)
					{
						spriteRenderer.sortingLayerName = "Sprites";
					}
				}
			}

			if(GameObject.Find("Cameras") == null)
			{
				GameObject prefab = Resources.Load("System/Cameras") as GameObject;
				GameObject newObject = Instantiate(prefab).gameObject;
				newObject.name = "Cameras";
				DontDestroyOnLoad(newObject);
			}

			Camera.main.GetComponent<RexCamera>().focusObject = GameManager.Instance.player.GetComponent<RexActor>();

			Destroy(gameObject);
		}
	}
}

