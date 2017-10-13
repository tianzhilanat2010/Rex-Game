/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace RexEngine
{
	public class RexSceneManager:MonoBehaviour 
	{
		public string startingLevel;

		[System.NonSerialized]
		public string previousLevel;

		[HideInInspector]
		public string levelToLoad;

		private bool willLoadSceneOnFadeoutComplete;

		[System.NonSerialized]
		public PlayerSpawnType playerSpawnType = PlayerSpawnType.SpawnPoint;

		[System.NonSerialized]
		public string loadPoint;

		//Used internally to keep the player's position consistent between scenes
		[System.NonSerialized]
		public float playerOffsetFromSceneLoader;

		//Used internally to keep the player's position consistent between scenes
		[System.NonSerialized]
		public Vector3 playerPositionOnSceneExit;

		protected bool willNextSceneFadeIn = true;

		public List<SceneLoader> sceneLoaders;

		public enum PlayerSpawnType
		{
			SceneLoader,
			SpawnPoint
		}

		private static RexSceneManager instance = null;
		public static RexSceneManager Instance 
		{ 
			get 
			{
				if(instance == null)
				{
					Debug.Log("RexSceneManager :: Instantiate");
					GameObject go = new GameObject();
					instance = go.AddComponent<RexSceneManager>();
					go.name = "Scene Manager";
				}

				return instance; 
			} 
		}

		void Awake()
		{
			if(instance == null)
			{
				instance = this;
			}

			DontDestroyOnLoad(gameObject);

			sceneLoaders = new List<SceneLoader>();
		}

		//Fades out the screen and loads in a new scene
		public void LoadSceneWithFadeOut(string level, Color fadeColor = default(Color), bool willStartFreshFade = true, bool _willNextSceneFadeIn = true)
		{
			StopAllCoroutines();

			GameManager.Instance.player.isBeingLoadedIntoNewScene = true;
			playerPositionOnSceneExit = GameManager.Instance.player.transform.position;
			willNextSceneFadeIn = _willNextSceneFadeIn;
			levelToLoad = level;
			previousLevel = Application.loadedLevelName;
			willLoadSceneOnFadeoutComplete = true;
			PurgeSceneLoaders();
			ScreenFade.Instance.Fade(ScreenFade.FadeType.Out, ScreenFade.FadeDuration.Medium, fadeColor, willStartFreshFade);

			StartCoroutine("LoadNewSceneCoroutine");
		}

		public void MovePlayerToSpawnPoint()
		{
			StartCoroutine("MovePlayerToSpawnPointCoroutine");
		}

		public string GetCurrentLevel()
		{
			return (levelToLoad == "") ? Application.loadedLevelName : levelToLoad;
		}

		public string GetLoadPoint()
		{
			return loadPoint;
		}

		public void RegisterSceneLoader(SceneLoader sceneLoader)
		{
			sceneLoaders.Add(sceneLoader);
		}

		public void UnregisterSceneLoader(SceneLoader sceneLoader)
		{
			sceneLoaders.Remove(sceneLoader);
		}

		protected IEnumerator MovePlayerToSpawnPointCoroutine()
		{
			Collider2D playerCollider = GameManager.Instance.player.GetComponent<Collider2D>();
			Collider2D controllerCollider = GameManager.Instance.player.slots.controller.GetComponent<Collider2D>();
			if(playerCollider) //We toggled the player's collider off to prevent unwanted collisions as the scene loaded; here, we toggle it back on
			{
				playerCollider.enabled = true;
			}

			if(controllerCollider) //We toggled the player's collider off to prevent unwanted collisions as the scene loaded; here, we toggle it back on
			{
				controllerCollider.enabled = true;
			}

			if(playerSpawnType == PlayerSpawnType.SpawnPoint) //The player is loading into a spawn point or checkpoint
			{
				Vector3 loadPoint = new Vector3(0, 0, 0);

				bool isSpawningIntoCheckpoint = false;
				foreach(Checkpoint checkpoint in FindObjectsOfType<Checkpoint>())
				{
					if(checkpoint.id == DataManager.Instance.lastCheckpointID) //We found the checkpoint the player should spawn at
					{
						isSpawningIntoCheckpoint = true;
						GameManager.Instance.OnPlayerSpawn();
						loadPoint = checkpoint.transform.position;
						float playerScale = (checkpoint.playerSpawnDirection == Direction.Horizontal.Right) ? 1.0f : -1.0f;
						GameManager.Instance.player.transform.localScale = new Vector3(playerScale, 1.0f, 1.0f);
					}
				}

				if(!isSpawningIntoCheckpoint) //If the scene we're loading doesn't have a checkpoint, check for a spawn point or simply load them in at Vector3.zero
				{
					GameObject playerSpawnPoint = GameObject.Find("PlayerSpawnPoint");
					Vector3 spawnPosition = (playerSpawnPoint) ? playerSpawnPoint.transform.position : Vector3.zero;
					GameManager.Instance.player.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
					loadPoint = spawnPosition;
				}

				GameManager.Instance.player.GetComponent<RexActor>().SetPosition(new Vector2(loadPoint.x, loadPoint.y));
				GameManager.Instance.player.GetComponent<RexActor>().loadedIntoScenePoint = loadPoint;
			}
			else //The player went through a SceneLoader and is loading into a new room
			{
				for(int i = 0; i < sceneLoaders.Count; i++)
				{
					if(sceneLoaders[i].identifier == loadPoint)
					{
						SceneLoader sceneLoader = sceneLoaders[i];

						Vector3 playerMovementDuringLoad = playerPositionOnSceneExit - new Vector3(0, 0, 0) - GameManager.Instance.player.transform.position;
						Vector3 position = sceneLoader.transform.position;
						float offset = 1.0f;

						if(!sceneLoader.isAttachedToGameObject)
						{
							if(sceneLoader.edge == SceneBoundary.Edge.Left)
							{
								position = new Vector3(position.x + offset + playerCollider.bounds.size.x / 2.0f + sceneLoader.GetComponent<Collider2D>().bounds.size.x / 2.0f + playerCollider.offset.x, position.y + playerOffsetFromSceneLoader - playerMovementDuringLoad.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.Right)
							{
								position = new Vector3(position.x - offset - playerCollider.bounds.size.x / 2.0f - sceneLoader.GetComponent<Collider2D>().bounds.size.x / 2.0f - playerCollider.offset.x, position.y + playerOffsetFromSceneLoader - playerMovementDuringLoad.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.Top)
							{
								position = new Vector3(position.x + playerOffsetFromSceneLoader - playerMovementDuringLoad.x, position.y - offset - playerCollider.bounds.size.y / 2.0f - sceneLoader.GetComponent<Collider2D>().bounds.size.y / 2.0f - playerCollider.offset.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.Bottom)
							{
								position = new Vector3(position.x + playerOffsetFromSceneLoader - playerMovementDuringLoad.x, position.y + offset + playerCollider.bounds.size.y / 2.0f + sceneLoader.GetComponent<Collider2D>().bounds.size.y / 2.0f + playerCollider.offset.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.None)
							{
								position = new Vector3(position.x, position.y, position.z);
							}
						}
						else
						{
							offset = 0.0f;
							if(sceneLoader.edge == SceneBoundary.Edge.Left)
							{
								position = new Vector3(position.x + offset + playerCollider.bounds.size.x / 2.0f + sceneLoader.GetComponent<Collider2D>().bounds.size.x / 2.0f + playerCollider.offset.x, position.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.Right)
							{
								position = new Vector3(position.x - offset - playerCollider.bounds.size.x / 2.0f - sceneLoader.GetComponent<Collider2D>().bounds.size.x / 2.0f - playerCollider.offset.x, position.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.Top)
							{
								position = new Vector3(position.x, position.y + offset + playerCollider.bounds.size.y / 2.0f + sceneLoader.GetComponent<Collider2D>().bounds.size.y / 2.0f + playerCollider.offset.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.Bottom)
							{
								position = new Vector3(position.x, position.y - offset - playerCollider.bounds.size.y / 2.0f - sceneLoader.GetComponent<Collider2D>().bounds.size.y / 2.0f - playerCollider.offset.y, position.z);
							}
							else if(sceneLoader.edge == SceneBoundary.Edge.None)
							{
								position = new Vector3(position.x, position.y, position.z);
							}

							Door door = sceneLoader.GetComponent<Door>();
							if(door != null)
							{
								door.ExitDoor();
							}
						}


						GameManager.Instance.player.loadedIntoScenePoint = position;
						GameManager.Instance.player.SetPosition(position);
						//GameManager.Instance.player.OnLoadedIntoNewScene();

						break;
					}
				}
			}

			yield return new WaitForSeconds(0.01f);

			GameManager.Instance.player.isBeingLoadedIntoNewScene = false;
		}

		public void DestroyScene()
		{
			//Projectiles need to be cleared separately, since they auto-parent themselves to the Projectiles GameObject which is in DontDestroyOnLoad
			GameObject projectiles = GameObject.Find("Projectiles");
			if(projectiles)
			{
				foreach(Projectile projectile in projectiles.GetComponentsInChildren<Projectile>())
				{
					if(projectile.willDestroyWhenSceneChanges)
					{
						projectile.Clear();
					}
				}
			}

			foreach(GameObject foundObject in GameObject.FindObjectsOfType<GameObject>())
			{
				if(foundObject.scene.name != "DontDestroyOnLoad")
				{
					Destroy(foundObject);
				}
			}
		}

		protected IEnumerator LoadNewSceneCoroutine()
		{
			float duration = ScreenFade.Instance.currentFadeDuration; //Wait until the screen fades out completely
			yield return new WaitForSeconds(duration);

			if(willLoadSceneOnFadeoutComplete) //Destroy the existing scene, and load the next one
			{
				bool willLoadAsync = true;
				#if (UNITY_WEBGL || UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
				willLoadAsync = false;
				#endif

				willLoadSceneOnFadeoutComplete = false;
				PhysicsManager.Instance.isSceneLoading = true;
				GameManager.Instance.player.GetComponent<BoxCollider2D>().enabled = false; //Toggle the player's collider off to prevent unwanted collisions as the new scene loads (but before the player has been moved to the proper position in the new scene)
				Collider2D controllerCollider = GameManager.Instance.player.slots.controller.GetComponent<Collider2D>();
				if(controllerCollider) 
				{
					controllerCollider.enabled = false;
				}


				if(willLoadAsync)
				{
					DestroyScene();
					AsyncOperation async = SceneManager.LoadSceneAsync(levelToLoad, LoadSceneMode.Additive); //Use Additive to circumvent a Unity bug that drops controller/keyboard inputs between scenes
					async.allowSceneActivation = false;

					while(!async.isDone)
					{
						yield return new WaitForEndOfFrame();

						if(async.progress >= 0.9f) //0.9 is as far as it needs to go for loading the scene itself; because of a Unity bug, it often won't hit 1.0
						{				
							async.allowSceneActivation = true;
							break;
						}
					}
				}
				else
				{
					SceneManager.LoadScene(levelToLoad); //Use Additive to circumvent a Unity bug that drops controller/keyboard inputs between scenes
					yield return new WaitForSeconds(0.05f);
				}

				StartCoroutine("OnSceneLoadComplete");
			}
		}

		protected IEnumerator OnSceneLoadComplete()
		{
			yield return new WaitForSeconds(0.1f);

			System.GC.Collect();

			if(willNextSceneFadeIn) //Fade the new scene in
			{
				yield return new WaitForSeconds(0.2f);

				FadeSceneIn();
			}

			PhysicsManager.Instance.isSceneLoading = false;

			SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(0).name);

			//Move the player to the spawn point or to the SceneLoader that corresponds to the SceneLoader they exited the last scene from, if applicable
			StartCoroutine("MovePlayerToSpawnPointCoroutine");
		}

		protected void FadeSceneIn()
		{
			ScreenFade.Instance.Fade(ScreenFade.FadeType.In, ScreenFade.FadeDuration.Short);
		}

		//Called automatically when a scene is destroyed; cleans up the SceneLoaders from the scene
		private void PurgeSceneLoaders()
		{
			for(int i = sceneLoaders.Count - 1; i >= 0; i--)
			{
				sceneLoaders.RemoveAt(i);
			}
		}
	}
}
