/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RexEngine
{
	public class GameManager:MonoBehaviour 
	{
		public RexActor playerPrefab;

		[System.NonSerialized]
		public bool useDebugInvincibility;

		[System.NonSerialized]
		public RexActor player;

		public RexInput input;

		public string gameOverScene = "Demo_GameOver";

		private static GameManager instance = null;
		public static GameManager Instance 
		{ 
			get 
			{
				if(instance == null)
				{
					GameObject go = new GameObject();
					instance = go.AddComponent<GameManager>();
					go.name = "GameManager";
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

			useDebugInvincibility = false;

			DontDestroyOnLoad(gameObject);
		}

		public void Init()
		{

		}

		void Start()
		{
			#if UNITY_EDITOR
			useDebugInvincibility = EditorPrefs.GetBool("DebugInvincibility");
			#endif

			if(useDebugInvincibility)
			{
				GameManager.Instance.useDebugInvincibility = true;
				player.GetComponent<RexActor>().invincibility.isDebugInvincibilityActive = true;
			}
		}

		public void OnPlayerSpawn()
		{
			StartCoroutine("PlayerSpawnCoroutine");
		}

		protected IEnumerator PlayerSpawnCoroutine()
		{
			player.slots.input.isEnabled = false;
			ReadyMessage.Instance.Show();
			yield return new WaitForSeconds(3.5f);

			player.slots.input.isEnabled = true;
		}

		public void OnPlayerDeath()
		{
			StartCoroutine("PlayerDeathCoroutine");
		}

		public void OnPlayerEnteredBottomlessPit()
		{
			if(!player.isDead)
			{
				player.KillImmediately();
			}
		}

		protected IEnumerator PlayerDeathCoroutine()
		{
			DataManager.Instance.deaths ++;

			DialogueManager.Instance.Hide();
			RexSoundManager.Instance.Pause();

			yield return new WaitForSeconds(2.0f);

			RexSceneManager.Instance.playerSpawnType = RexSceneManager.PlayerSpawnType.SpawnPoint;

			ScreenFade.Instance.Fade(ScreenFade.FadeType.Out, ScreenFade.FadeDuration.Short, Color.white);
			yield return new WaitForSeconds(ScreenFade.Instance.currentFadeDuration);

			if(player.slots.spriteHolder)
			{
				player.slots.spriteHolder.gameObject.SetActive(false);
			}

			PhysicsManager.Instance.gravityScale = 1.0f;
			player.slots.physicsObject.SetVelocityX(0.0f);
			player.slots.physicsObject.SetVelocityY(0.0f);

			ScoreManager.Instance.RevertToLastCheckpointScore();
			DataManager.Instance.Load();

			player.Reset();

			bool isGameOver = LivesManager.Instance.DecrementLives(1);

			if(!isGameOver) //The player has lives remaining; return to the last checkpoint
			{
				RexSceneManager.Instance.LoadSceneWithFadeOut(DataManager.Instance.lastSavedScene, Color.white, false, false);

				yield return new WaitForSeconds(1.0f);

				player.Revive();
			}
			else //The player has no more lives; go to the Game Over scene
			{
				RexSceneManager.Instance.LoadSceneWithFadeOut(gameOverScene, Color.white, false, false);

				yield return new WaitForSeconds(1.0f);
			}

			yield return new WaitForSeconds(0.5f);

			ScreenFade.Instance.Fade(ScreenFade.FadeType.In);
		}

		public void SetSavedScene(string _lastSavedScene, string _checkpointID)
		{
			DataManager.Instance.lastSavedScene = _lastSavedScene;
			DataManager.Instance.lastCheckpointID = _checkpointID;
		}

		public void MakePlayerActive()
		{
			player.gameObject.SetActive(true);
		}

		public void MakePlayerInactive()
		{
			player.CancelInvoke();
			player.gameObject.SetActive(false);
		}
	}
}
