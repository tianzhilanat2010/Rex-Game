/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RexEngine
{
	public class RexMenu:MonoBehaviour 
	{
		#if UNITY_EDITOR
		[MenuItem("GameObject/Create Rex Engine/Create Blank Player", false, 100)]
		private static void CreateBlankPlayer()
		{
			Object prefab = Resources.Load("Templates/PlayerTemplate") as Object;
			GameObject playerGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
			playerGameObject.name = "Player";
			PrefabUtility.DisconnectPrefabInstance(playerGameObject);

			Selection.activeGameObject = playerGameObject;
		}

		[MenuItem("GameObject/Create Rex Engine/Create Blank Enemy", false, 110)]
		private static void CreateBlankEnemy()
		{
			Object prefab = Resources.Load("Templates/EnemyTemplate") as Object;
			GameObject enemyGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
			enemyGameObject.name = "Enemy";
			PrefabUtility.DisconnectPrefabInstance(enemyGameObject);

			Selection.activeGameObject = enemyGameObject;
		}

		[MenuItem("GameObject/Create Rex Engine/Create Terrain", false, 120)]
		private static void CreateTerrain()
		{
			Object terrainPrefab = Resources.Load("System/_Default/FloatingTerrain") as Object;
			GameObject terrainGameObject = Instantiate(terrainPrefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
			terrainGameObject.name = "Terrain";
			PrefabUtility.DisconnectPrefabInstance(terrainGameObject);

			if(GameObject.Find("Terrain") != null)
			{
				terrainGameObject.transform.parent = GameObject.Find("Terrain").transform;
			}

			Selection.activeGameObject = terrainGameObject;
		}

		[MenuItem("GameObject/Create Rex Engine/Scene Loader/Create Left Scene Loader", false, 1)]
		private static void CreateLeftSceneLoader()
		{
			Object prefab = Resources.Load("System/SceneLoader_left") as Object;
			GameObject loaderGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
			loaderGameObject.name = "SceneLoader_left";
			PrefabUtility.DisconnectPrefabInstance(loaderGameObject);

			Selection.activeGameObject = loaderGameObject;
		}

		[MenuItem("GameObject/Create Rex Engine/Scene Loader/Create Right Scene Loader", false, 1)]
		private static void CreateRightSceneLoader()
		{
			Object prefab = Resources.Load("System/SceneLoader_right") as Object;
			GameObject loaderGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
			loaderGameObject.name = "SceneLoader_right";
			PrefabUtility.DisconnectPrefabInstance(loaderGameObject);

			Selection.activeGameObject = loaderGameObject;
		}

		[MenuItem("GameObject/Create Rex Engine/Scene Loader/Create Top Scene Loader", false, 1)]
		private static void CreateTopSceneLoader()
		{
			Object prefab = Resources.Load("System/SceneLoader_top") as Object;
			GameObject loaderGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
			loaderGameObject.name = "SceneLoader_top";
			PrefabUtility.DisconnectPrefabInstance(loaderGameObject);

			Selection.activeGameObject = loaderGameObject;
		}

		[MenuItem("GameObject/Create Rex Engine/Scene Loader/Create Bottom Scene Loader", false, 1)]
		private static void CreateBottomSceneLoader()
		{
			Object prefab = Resources.Load("System/SceneLoader_bottom") as Object;
			GameObject loaderGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
			loaderGameObject.name = "SceneLoader_bottom";
			PrefabUtility.DisconnectPrefabInstance(loaderGameObject);

			Selection.activeGameObject = loaderGameObject;
		}

		[MenuItem("Tools/Rex Engine/Setup Level Scene", false, 100)]
		private static void SetupLevelScene()
		{
			GameObject defaultMainCamera = GameObject.Find("Main Camera");
			if(defaultMainCamera != null)
			{
				DestroyImmediate(defaultMainCamera);
			}

			Object setupPrefab = Resources.Load("System/Setup") as Object;
			GameObject setupGameObject = PrefabUtility.InstantiatePrefab(setupPrefab) as GameObject;

			Object levelManagerPrefab = Resources.Load("System/_Default/LevelManager") as Object;
			GameObject levelManagerGameObject = PrefabUtility.InstantiatePrefab(levelManagerPrefab) as GameObject;

			GameObject gizmos = CreateGizmos();

			Object startingTerrainPrefab = Resources.Load("System/_Default/Terrain") as Object;
			GameObject startingTerrainGameObject = Instantiate(startingTerrainPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity) as GameObject;
			startingTerrainGameObject.name = "Terrain";
			Selection.activeGameObject = startingTerrainGameObject;

			GameObject actors = new GameObject();
			actors.name = "Actors";

			setupGameObject.transform.SetSiblingIndex(0);
			levelManagerGameObject.transform.SetSiblingIndex(1);
			gizmos.transform.SetSiblingIndex(2);

			Object bgPrefab = Resources.Load("System/_Default/BG") as Object;
			GameObject bgGameObject = Instantiate(bgPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity) as GameObject;
			bgGameObject.name = "BG";

			Debug.Log("Rex Engine level scene setup!");
		}

		private static GameObject CreateGizmos()
		{
			GameObject gizmos = new GameObject();
			gizmos.name = "Gizmos";

			Object playerSpawnPointPrefab = Resources.Load("System/PlayerSpawnPoint") as Object;
			GameObject playerSpawnPointGameObject = PrefabUtility.InstantiatePrefab(playerSpawnPointPrefab) as GameObject;
			playerSpawnPointGameObject.transform.parent = gizmos.transform;

			Object cameraBoundaryLeftPrefab = Resources.Load("System/_SceneBoundary/SceneBoundary_left") as Object;
			GameObject cameraBoundaryLeftGameObject = PrefabUtility.InstantiatePrefab(cameraBoundaryLeftPrefab) as GameObject;
			cameraBoundaryLeftGameObject.transform.parent = gizmos.transform;
			cameraBoundaryLeftGameObject.transform.position = new Vector3(-12.9f, 0.0f, 0.0f);

			Object cameraBoundaryRightPrefab = Resources.Load("System/_SceneBoundary/SceneBoundary_right") as Object;
			GameObject cameraBoundaryRightGameObject = PrefabUtility.InstantiatePrefab(cameraBoundaryRightPrefab) as GameObject;
			cameraBoundaryRightGameObject.transform.parent = gizmos.transform;
			cameraBoundaryRightGameObject.transform.position = new Vector3(12.9f, 0.0f, 0.0f);

			Object cameraBoundaryTopPrefab = Resources.Load("System/_SceneBoundary/SceneBoundary_top") as Object;
			GameObject cameraBoundaryTopGameObject = PrefabUtility.InstantiatePrefab(cameraBoundaryTopPrefab) as GameObject;
			cameraBoundaryTopGameObject.transform.parent = gizmos.transform;
			cameraBoundaryTopGameObject.transform.position = new Vector3(0.0f, 7.275f, 0.0f);

			Object cameraBoundaryBottomPrefab = Resources.Load("System/_SceneBoundary/SceneBoundary_bottom") as Object;
			GameObject cameraBoundaryBottomGameObject = PrefabUtility.InstantiatePrefab(cameraBoundaryBottomPrefab) as GameObject;
			cameraBoundaryBottomGameObject.transform.parent = gizmos.transform;
			cameraBoundaryBottomGameObject.transform.position = new Vector3(0.0f, -7.275f, 0.0f);

			return gizmos;
		}

		private static GameObject CreateCameras()
		{
			GameObject cameras = new GameObject();
			cameras.name = "Cameras";

			Object uiCameraPrefab = Resources.Load("System/_Camera/UICamera") as Object;
			GameObject uiCameraGameObject = PrefabUtility.InstantiatePrefab(uiCameraPrefab) as GameObject;
			uiCameraGameObject.transform.parent = cameras.transform;

			Object foregroundCameraPrefab = Resources.Load("System/_Camera/ForegroundCamera") as Object;
			GameObject foregroundCameraGameObject = PrefabUtility.InstantiatePrefab(foregroundCameraPrefab) as GameObject;
			foregroundCameraGameObject.transform.parent = cameras.transform;

			Object mainCameraPrefab = Resources.Load("System/_Camera/MainCamera") as Object;
			GameObject mainCameraGameObject = PrefabUtility.InstantiatePrefab(mainCameraPrefab) as GameObject;
			mainCameraGameObject.transform.parent = cameras.transform;
			Selection.activeGameObject = mainCameraGameObject;

			Object midgroundCameraPrefab = Resources.Load("System/_Camera/MidgroundCamera") as Object;
			GameObject midgroundCameraGameObject = PrefabUtility.InstantiatePrefab(midgroundCameraPrefab) as GameObject;
			midgroundCameraGameObject.transform.parent = cameras.transform;

			Object backgroundCameraPrefab = Resources.Load("System/_Camera/BackgroundCamera") as Object;
			GameObject backgroundCameraGameObject = PrefabUtility.InstantiatePrefab(backgroundCameraPrefab) as GameObject;
			backgroundCameraGameObject.transform.parent = cameras.transform;

			Object canvasCameraPrefab = Resources.Load("System/_Camera/CanvasCamera") as Object;
			GameObject canvasCameraGameObject = PrefabUtility.InstantiatePrefab(canvasCameraPrefab) as GameObject;
			canvasCameraGameObject.transform.parent = cameras.transform;

			mainCameraGameObject.GetComponent<RexCamera>().foregroundCamera = foregroundCameraGameObject.GetComponent<Camera>();
			mainCameraGameObject.GetComponent<RexCamera>().backgroundCamera = backgroundCameraGameObject.GetComponent<Camera>();
			mainCameraGameObject.GetComponent<RexCamera>().midgroundCamera = midgroundCameraGameObject.GetComponent<Camera>();

			return cameras;
		}

		[MenuItem("Tools/Rex Engine/Clear PlayerPrefs", false, 400)]
		private static void ClearPlayerPrefs()
		{
			PlayerPrefs.DeleteAll();
			Debug.Log("PlayerPrefs cleared!");
		}

		[MenuItem("Tools/Rex Engine/Enable Debug Invincibility", false, 200)]
		private static void EnableDebugInvincibility()
		{
			EditorPrefs.SetBool("DebugInvincibility", true);
			Debug.Log("Debug invincibility: " + EditorPrefs.GetBool("DebugInvincibility"));
		}

		[MenuItem("Tools/Rex Engine/Enable Debug Invincibility", true)]
		private static bool CanEnableDebugInvincibility()
		{
			return !EditorPrefs.GetBool("DebugInvincibility");
		}

		[MenuItem("Tools/Rex Engine/Disable Debug Invincibility", false, 200)]
		private static void DisableDebugInvincibility()
		{
			EditorPrefs.SetBool("DebugInvincibility", false);
			Debug.Log("Debug invincibility: " + EditorPrefs.GetBool("DebugInvincibility"));
		}

		[MenuItem("Tools/Rex Engine/Disable Debug Invincibility", true)]
		private static bool CanDisableDebugInvincibility()
		{
			return EditorPrefs.GetBool("DebugInvincibility");
		}

		//

		[MenuItem("Tools/Rex Engine/Enable Music", false, 250)]
		private static void EnableMusic()
		{
			EditorPrefs.SetBool("IsMusicEnabled", true);
			Debug.Log("Music: " + EditorPrefs.GetBool("IsMusicEnabled"));
		}

		[MenuItem("Tools/Rex Engine/Enable Music", true)]
		private static bool CanEnableMusic()
		{
			return !EditorPrefs.GetBool("IsMusicEnabled");
		}

		[MenuItem("Tools/Rex Engine/Disable Music", false, 250)]
		private static void DisableMusic()
		{
			EditorPrefs.SetBool("IsMusicEnabled", false);
			Debug.Log("Music: " + EditorPrefs.GetBool("IsMusicEnabled"));
		}

		[MenuItem("Tools/Rex Engine/Disable Music", true)]
		private static bool CanDisableMusic()
		{
			return EditorPrefs.GetBool("IsMusicEnabled");
		}

		[MenuItem("Tools/Rex Engine/Add Rex Engine Tags and Layers", false, 350)]
		private static void AddRexEngineTagsAndLayers()
		{
			AddRexEngineTags();
			AddRexEngineLayers();
			AddRexEngineSortingLayers();
		}

		//[MenuItem("Tools/Rex Engine/Add Rex Engine Tags", false, 350)]
		private static void AddRexEngineTags()
		{
			SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			SerializedProperty tagsProperty = tagManager.FindProperty("tags");

			List<string> tagsToAdd = new List<string>();
			tagsToAdd.Add("Terrain");
			tagsToAdd.Add("Enemy");
			tagsToAdd.Add("Projectile");
			tagsToAdd.Add("Reflector");
			tagsToAdd.Add("Ladder");

			for(int i = 0; i < tagsToAdd.Count; i ++)
			{
				bool isTagAlreadyPresent = false;
				for(int j = 0; j < tagsProperty.arraySize; j ++)
				{
					SerializedProperty t = tagsProperty.GetArrayElementAtIndex(j);
					if(t.stringValue.Equals(tagsToAdd[i])) 
					{ 
						isTagAlreadyPresent = true; 
						break; 
					}
				}

				if(!isTagAlreadyPresent)
				{
					tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize - 1);
					SerializedProperty serializedTags = tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1);
					serializedTags.stringValue = tagsToAdd[i];
				}
			}

			tagManager.ApplyModifiedProperties();
		}

		//[MenuItem("Tools/Rex Engine/Add Rex Engine Layers", false, 360)]
		private static void AddRexEngineLayers()
		{
			SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			SerializedProperty layersProperty = tagManager.FindProperty("layers");

			List<string> layersToAdd = new List<string>();
			layersToAdd.Add("Terrain");
			layersToAdd.Add("PassThroughBottom");
			layersToAdd.Add("Player");
			layersToAdd.Add("Boundaries");
			layersToAdd.Add("Canvas");
			layersToAdd.Add("Background");
			layersToAdd.Add("Midground");
			layersToAdd.Add("Foreground");

			for(int i = 0; i < layersToAdd.Count; i ++)
			{
				bool doesLayerNameAlreadyExist = false;
				for(int j = 0; j < layersProperty.arraySize; j ++)
				{
					if(layersProperty.GetArrayElementAtIndex(j).stringValue.Equals(layersToAdd[i]))
					{
						doesLayerNameAlreadyExist = true;
					}
				}

				int indexToAddAt = 8; //Start at 8 because the first 7 layers are taken by default system layers
				bool didFindEmptyIndex = false;
				for(int j = indexToAddAt; j < layersProperty.arraySize; j ++)
				{
					if(layersProperty.GetArrayElementAtIndex(j).stringValue == "")
					{
						didFindEmptyIndex = true;
						indexToAddAt = j;
						break;
					}
				}

				SerializedProperty sp = layersProperty.GetArrayElementAtIndex(indexToAddAt);
				if(sp != null && !doesLayerNameAlreadyExist && didFindEmptyIndex) 
				{
					sp.stringValue = layersToAdd[i];
				}
				else
				{
					Debug.Log("Unable to add new layer named " + layersToAdd[i] + "; either all layers are already taken or this layer already exists.");
				}
			}

			tagManager.ApplyModifiedProperties();
		}

		//[MenuItem("Tools/Rex Engine/Add Rex Engine SortingLayers", false, 365)]
		private static void AddRexEngineSortingLayers()
		{
			SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			SerializedProperty layersProperty = tagManager.FindProperty("m_SortingLayers");

			List<string> layersToAdd = new List<string>();
			layersToAdd.Add("Canvas");
			layersToAdd.Add("Background");
			layersToAdd.Add("Midground");
			layersToAdd.Add("Sprites");
			layersToAdd.Add("Foreground");
			layersToAdd.Add("Particles");
			layersToAdd.Add("UI");

			for(int i = 0; i < layersToAdd.Count; i ++)
			{
				bool doesLayerNameAlreadyExist = false;
				for(int j = 0; j < layersProperty.arraySize; j ++)
				{
					if(layersProperty.GetArrayElementAtIndex(j).FindPropertyRelative("name").stringValue.Equals(layersToAdd[i]))
					{
						doesLayerNameAlreadyExist = true;
						layersToAdd[i] = "null";
					}
				}

				int indexToAddAt = layersProperty.arraySize;
				if(layersToAdd[i] != "null")
				{
					layersProperty.InsertArrayElementAtIndex(indexToAddAt);
					SerializedProperty sp = layersProperty.GetArrayElementAtIndex(indexToAddAt);
					sp.FindPropertyRelative("name").stringValue = layersToAdd[i];
					sp.FindPropertyRelative("uniqueID").intValue = i + 10;
				}
			}

			tagManager.ApplyModifiedProperties();
		}

		private static GameObject GetSelectGameObject()
		{
			GameObject selectedObject = Selection.activeGameObject;
			RexController rexController = null;
			if(selectedObject)
			{
				rexController = selectedObject.GetComponent<RexController>();
			}

			if(selectedObject && rexController)
			{
				return selectedObject;
			}
			else
			{
				EditorUtility.DisplayDialog("Ruh-roh!", "Please select a GameObject with a RexController component to attach this ability to first.", "Got it!");
				return null;
			}
		}

		[MenuItem("Tools/Rex Engine/Add Attack/Add Melee Attack", false, 0)]
		private static void AddMeleeAttack()
		{
			GameObject selectedObject = Selection.activeGameObject;
			RexActor actor = null;
			if(selectedObject)
			{
				actor = selectedObject.GetComponent<RexActor>();
			}

			GameObject attackGameObject = null;
			if(actor != null)
			{
				Transform attackTransform = actor.transform.Find("Attacks");
				if(attackTransform)
				{
					attackGameObject = attackTransform.gameObject;
				}
			}

			if(attackGameObject == null && actor != null)
			{
				attackGameObject = new GameObject();
				attackGameObject.name = "Attacks";
				attackGameObject.transform.parent = actor.transform;
			}

			if(actor != null)
			{
				Object prefab = Resources.Load("Templates/MeleeAttackTemplate") as Object;
				GameObject meleeGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
				meleeGameObject.name = "MeleeAttack";
				PrefabUtility.DisconnectPrefabInstance(meleeGameObject);
				meleeGameObject.transform.parent = attackGameObject.transform;
				Selection.activeGameObject = meleeGameObject;

				Attack attack = meleeGameObject.GetComponent<Attack>();
				attack.slots.actor = actor;
				attack.slots.animator = actor.slots.anim;
				attack.slots.audio = actor.GetComponent<AudioSource>();
				attack.slots.boxCollider = attack.GetComponent<BoxCollider2D>();
				attack.slots.spriteRenderer = attack.GetComponentInChildren<SpriteRenderer>();

				if(actor.tag == "Enemy")
				{
					attack.GetComponent<ContactDamage>().willDamagePlayer = true;
					attack.GetComponent<ContactDamage>().willDamageEnemies = false;
				}
			}
			else
			{
				EditorUtility.DisplayDialog("Ruh-roh!", "Please select a GameObject with a RexActor component to attach this attack to first.", "Got it!");
			}
		}

		[MenuItem("Tools/Rex Engine/Add Attack/Add Projectile Attack", false, 0)]
		private static void AddProjectileAttack()
		{
			GameObject selectedObject = Selection.activeGameObject;
			RexActor actor = null;
			if(selectedObject)
			{
				actor = selectedObject.GetComponent<RexActor>();
			}

			GameObject attackGameObject = null;
			if(actor != null)
			{
				Transform attackTransform = actor.transform.Find("Attacks");
				if(attackTransform)
				{
					attackGameObject = attackTransform.gameObject;
				}
			}

			if(attackGameObject == null && actor != null)
			{
				attackGameObject = new GameObject();
				attackGameObject.name = "Attacks";
				attackGameObject.transform.parent = actor.transform;
			}

			if(actor != null)
			{
				Object prefab = Resources.Load("Templates/ProjectileAttackTemplate") as Object;
				GameObject projectileGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
				projectileGameObject.name = "ProjectileAttack";
				PrefabUtility.DisconnectPrefabInstance(projectileGameObject);
				projectileGameObject.transform.parent = attackGameObject.transform;
				Selection.activeGameObject = projectileGameObject;
				projectileGameObject.transform.localPosition = new Vector3(1.94f, 0.0f, 0.0f);

				Attack attack = projectileGameObject.GetComponent<Attack>();
				attack.slots.actor = actor;
				attack.slots.audio = actor.GetComponent<AudioSource>();
				attack.slots.boxCollider = attack.GetComponent<BoxCollider2D>();
				attack.slots.spriteRenderer = attack.GetComponentInChildren<SpriteRenderer>();

				if(actor.tag == "Enemy")
				{
					attack.GetComponent<ContactDamage>().willDamagePlayer = true;
					attack.GetComponent<ContactDamage>().willDamageEnemies = false;
				}
			}
			else
			{
				EditorUtility.DisplayDialog("Ruh-roh!", "Please select a GameObject with a RexActor component to attach this attack to first.", "Got it!");
			}
		}

		[MenuItem("Tools/Rex Engine/Add Attack/Add Charge Projectile Attack", false, 0)]
		private static void AddChargeProjectileAttack()
		{
			GameObject selectedObject = Selection.activeGameObject;
			RexActor actor = null;
			if(selectedObject)
			{
				actor = selectedObject.GetComponent<RexActor>();
			}

			GameObject attackGameObject = null;
			if(actor != null)
			{
				Transform attackTransform = actor.transform.Find("Attacks");
				if(attackTransform)
				{
					attackGameObject = attackTransform.gameObject;
				}
			}

			if(attackGameObject == null && actor != null)
			{
				attackGameObject = new GameObject();
				attackGameObject.name = "Attacks";
				attackGameObject.transform.parent = actor.transform;
			}

			if(actor != null)
			{
				Object prefab = Resources.Load("Templates/ChargeProjectileAttackTemplate") as Object;
				GameObject projectileGameObject = Instantiate(prefab, SceneView.lastActiveSceneView.pivot, Quaternion.identity) as GameObject;
				projectileGameObject.name = "ChargeProjectileAttack";
				PrefabUtility.DisconnectPrefabInstance(projectileGameObject);
				projectileGameObject.transform.parent = attackGameObject.transform;
				Selection.activeGameObject = projectileGameObject;
				projectileGameObject.transform.localPosition = new Vector3(1.94f, 0.0f, 0.0f);

				Attack attack = projectileGameObject.GetComponent<Attack>();
				attack.slots.actor = actor;
				attack.slots.audio = actor.GetComponent<AudioSource>();
				attack.slots.boxCollider = attack.GetComponent<BoxCollider2D>();
				attack.slots.spriteRenderer = attack.GetComponentInChildren<SpriteRenderer>();

				ChargeProjectile chargeProjectile = attack.GetComponent<ChargeProjectile>();
				if(chargeProjectile != null)
				{
					chargeProjectile.spriteToFlash = actor.slots.spriteRenderer;
				}

				if(actor.tag == "Enemy")
				{
					attack.GetComponent<ContactDamage>().willDamagePlayer = true;
					attack.GetComponent<ContactDamage>().willDamageEnemies = false;
				}
			}
			else
			{
				EditorUtility.DisplayDialog("Ruh-roh!", "Please select a GameObject with a RexActor component to attach this attack to first.", "Got it!");
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Bounce", false, 0)]
		private static void AddBounceState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<BounceState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Crouching", false, 0)]
		private static void AddCrouchState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<CrouchState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Dashing", false, 00)]
		private static void AddDashState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<DashState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Jumping", false, 00)]
		private static void AddJumpState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<JumpState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Knockback", false, 0)]
		private static void AddKnockbackState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<KnockbackState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Ladder Climbing", false, 0)]
		private static void AddLadderState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<LadderState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Landing", false, 0)]
		private static void AddLandingState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<LandingState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Movement", false, 0)]
		private static void AddMovingState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<MovingState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Add Ability/Add Wall Cling", false, 0)]
		private static void AddWallClingState()
		{
			GameObject selectedObject = GetSelectGameObject();
			if(selectedObject)
			{
				RexState state = selectedObject.AddComponent<WallClingState>();
				state.controller = selectedObject.GetComponent<RexController>();
			}
		}

		[MenuItem("Tools/Rex Engine/Fill RexActor Slots", false, 50)]
		private static void FillRexActorSlots()
		{
			GameObject selectedObject = Selection.activeGameObject;
			RexActor actor = null;
			if(selectedObject)
			{
				actor = selectedObject.GetComponent<RexActor>();
			}

			if(actor != null)
			{
				actor.slots.physicsObject = actor.GetComponent<RexPhysics>();
				actor.slots.anim = actor.GetComponent<Animator>();
				actor.slots.controller = actor.GetComponentInChildren<RexController>();
				actor.slots.input = actor.GetComponent<RexInput>();
				actor.slots.jitter = actor.GetComponent<Jitter>();
				actor.slots.spriteHolder = actor.transform.Find("SpriteHolder");
				actor.slots.spriteRenderer = actor.transform.Find("SpriteHolder").GetComponentInChildren<SpriteRenderer>();

				if(actor.damagedProperties.spritesToFlash == null)
				{
					actor.damagedProperties.spritesToFlash = new List<SpriteRenderer>();
				}

				actor.damagedProperties.spritesToFlash.Add(actor.slots.spriteRenderer);
				actor.slots.collider = actor.GetComponent<BoxCollider2D>();

				RexPhysics physicsObject = actor.GetComponent<RexPhysics>();
				if(physicsObject != null)
				{
					physicsObject.rexObject = actor;
				}

				RexController controller = actor.slots.controller;
				if(controller)
				{
					controller.slots.actor = actor;
					actor.waterProperties.landController = controller;
				}

				Energy[] energy = actor.GetComponentsInChildren<Energy>();
				if(energy.Length > 0)
				{
					for(int i = 0; i < energy.Length; i ++)
					{
						if(energy[i].gameObject.name == "HP")
						{
							actor.hp = energy[i];
						}
						else if(energy[i].gameObject.name == "MP")
						{
							actor.mp = energy[i];
						}
					}

					if(actor.hp == null)
					{
						actor.hp = energy[0];
					}
				}

				Attack[] attacks = actor.GetComponentsInChildren<Attack>();
				for(int i = 0; i < attacks.Length; i ++)
				{
					attacks[i].slots.actor = actor;
				}

				EditorUtility.DisplayDialog("Yeah!", "The RexActor was successfully slotted!", "Yay!");
			}
			else
			{
				EditorUtility.DisplayDialog("Ruh-roh!", "Please select a GameObject with a RexActor component first.", "Got it!");
			}
		}

		[MenuItem("Tools/Rex Engine/Update Layering For Scene", false, 370)]
		private static void UpdateLayeringForScene()
		{
			Debug.Log("Rex Engine :: Update Layering For Scene");
			GameObject terrainObject = GameObject.Find("Terrain");
			if(terrainObject != null)
			{
				foreach(Transform terrain in terrainObject.transform.GetComponentsInChildren<Transform>())
				{
					string objectName = terrain.name.Split('_')[0];
					if(objectName == "OneWayPlatform")
					{
						terrain.gameObject.layer = LayerMask.NameToLayer("PassThroughBottom");
						terrain.gameObject.tag = "Terrain";

						SpriteRenderer spriteRenderer = terrain.GetComponent<SpriteRenderer>();
						if(spriteRenderer != null)
						{
							spriteRenderer.sortingLayerName = "Sprites";
						}
					}
					else
					{
						terrain.gameObject.layer = LayerMask.NameToLayer("Terrain");
						terrain.gameObject.tag = "Terrain";

						SpriteRenderer spriteRenderer = terrain.GetComponent<SpriteRenderer>();
						if(spriteRenderer != null)
						{
							spriteRenderer.sortingLayerName = "Sprites";
						}
					}
				}
			}

			GameObject actorsObject = GameObject.Find("Actors");
			if(actorsObject != null)
			{
				foreach(Transform actor in actorsObject.transform.GetComponentsInChildren<Transform>())
				{
					string objectName = actor.name.Split(' ')[0];
					if(objectName == "Tricerabot" || objectName == "Dactyl" || objectName == "Ankylosaur" || objectName == "NewAbilityButton")
					{
						actor.tag = "Enemy";
						actor.gameObject.layer = LayerMask.NameToLayer("Default");
					}
					else if(objectName == "Bolt" || objectName == "HPRestore" || objectName == "HPRestoreLarge")
					{
						actor.tag = "Untagged";
						actor.gameObject.layer = LayerMask.NameToLayer("Default");
					}

					SpriteRenderer spriteRenderer = actor.GetComponent<SpriteRenderer>();
					if(spriteRenderer != null)
					{
						spriteRenderer.sortingLayerName = "Sprites";
					}
				}
			}

			GameObject foregroundObject = GameObject.Find("Foreground");
			if(foregroundObject != null)
			{
				foreach(Transform sprite in foregroundObject.transform.GetComponentsInChildren<Transform>())
				{
					sprite.tag = "Untagged";
					sprite.gameObject.layer = LayerMask.NameToLayer("Foreground");
					SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();
					if(spriteRenderer != null)
					{
						Debug.Log("Found foreground sprite!");
						spriteRenderer.sortingLayerName = "Foreground";
					}
				}
			}

			GameObject decorObject = GameObject.Find("Decor");
			if(decorObject != null)
			{
				foreach(Transform sprite in decorObject.transform.GetComponentsInChildren<Transform>())
				{
					sprite.tag = "Untagged";
					sprite.gameObject.layer = LayerMask.NameToLayer("Default");

					SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();
					if(spriteRenderer != null)
					{
						spriteRenderer.sortingLayerName = "Sprites";
					}
				}
			}

			GameObject midgroundObject = GameObject.Find("Midground");
			if(midgroundObject != null)
			{
				foreach(Transform sprite in midgroundObject.transform.GetComponentsInChildren<Transform>())
				{
					sprite.tag = "Untagged";
					sprite.gameObject.layer = LayerMask.NameToLayer("Midground");

					SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();
					if(spriteRenderer != null)
					{
						spriteRenderer.sortingLayerName = "Midground";
					}
				}
			}

			GameObject backgroundObject = GameObject.Find("Background");
			if(backgroundObject != null)
			{
				foreach(Transform sprite in backgroundObject.transform.GetComponentsInChildren<Transform>())
				{
					sprite.tag = "Untagged";
					sprite.gameObject.layer = LayerMask.NameToLayer("Background");

					SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();
					if(spriteRenderer != null)
					{
						spriteRenderer.sortingLayerName = "Background";
					}
				}
			}

			GameObject canvasObject = GameObject.Find("Canvas");
			if(canvasObject != null)
			{
				foreach(Transform sprite in canvasObject.transform.GetComponentsInChildren<Transform>())
				{
					sprite.tag = "Untagged";
					sprite.gameObject.layer = LayerMask.NameToLayer("Canvas");

					SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();
					if(spriteRenderer != null)
					{
						spriteRenderer.sortingLayerName = "Canvas";
					}
				}
			}

			GameObject caveObject = GameObject.Find("Cave");
			if(caveObject != null)
			{
				foreach(Transform sprite in caveObject.transform.GetComponentsInChildren<Transform>())
				{
					sprite.tag = "Untagged";
					sprite.gameObject.layer = LayerMask.NameToLayer("Default");

					SpriteRenderer spriteRenderer = sprite.GetComponent<SpriteRenderer>();
					if(spriteRenderer != null)
					{
						spriteRenderer.sortingLayerName = "Sprites";
					}
				}
			}
		}

		[MenuItem("Tools/Rex Engine/View Online Documentation", false, 450)]
		private static void ViewOnlineDocumentation()
		{
			Application.OpenURL("http://www.skytyrannosaur.com/rexenginequickstart/");
		}
		#endif
	}

}
