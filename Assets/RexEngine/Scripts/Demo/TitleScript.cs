/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RexEngine;

public class TitleScript:MonoBehaviour 
{
	protected bool isExiting = false;

	void Awake() 
	{
		
	}

	void Start() 
	{
		RexTouchInput rexTouchInput = GameManager.Instance.player.GetComponent<RexTouchInput>();
		if(rexTouchInput != null)
		{
			rexTouchInput.ToggleTouchInterface(false);
		}

		ScoreManager.Instance.gameObject.SetActive(false);
		GameManager.Instance.player.gameObject.SetActive(false);

		if(GameManager.Instance.player.hp.bar)
		{
			GameManager.Instance.player.hp.bar.gameObject.SetActive(false);
		}

		ScoreManager.Instance.text.gameObject.SetActive(false);
		PauseManager.Instance.isPauseEnabled = false;
		LivesManager.Instance.Hide();
	}
	
	void Update() 
	{
		if(GameManager.Instance.input.isJumpButtonDownThisFrame || Input.GetMouseButtonDown(0))
		{
			if(!isExiting)
			{
				ExitToGame();
			}
		}
	}

	protected void ExitToGame()
	{
		isExiting = true;
		DataManager.Instance.ResetData();
		GameManager.Instance.player.gameObject.SetActive(true);

		if(GameManager.Instance.player.GetComponent<Booster>() != null)
		{
			GameManager.Instance.player.Reset();
		}

		GameManager.Instance.player.Revive();
		LivesManager.Instance.ResetLivesToStartingValue();
		RexSceneManager.Instance.playerSpawnType = RexSceneManager.PlayerSpawnType.SpawnPoint;
		GameManager.Instance.player.slots.input.isEnabled = true;
		GameManager.Instance.player.SetPosition(new Vector2(-10000.0f, -10000.0f));
		ScreenFade.Instance.Fade(ScreenFade.FadeType.Out, ScreenFade.FadeDuration.Short, Color.white);

		StartCoroutine("ExitToGameCoroutine");
	}

	protected IEnumerator ExitToGameCoroutine()
	{
		yield return new WaitForSeconds(0.5f);

		RexSceneManager.Instance.LoadSceneWithFadeOut("Demo_1", Color.white, false);

		yield return new WaitForSeconds(0.25f);

		#if UNITY_ANDROID || UNITY_IPHONE
		RexTouchInput rexTouchInput = GameManager.Instance.player.GetComponent<RexTouchInput>();
		if(rexTouchInput != null)
		{
			rexTouchInput.ToggleTouchInterface(true);
		}
		#endif

		ScoreManager.Instance.gameObject.SetActive(true);
		ScoreManager.Instance.SetScoreAtCheckpoint(0);
		ScoreManager.Instance.SetScore(0);

		if(GameManager.Instance.player.hp.bar)
		{
			GameManager.Instance.player.hp.bar.gameObject.SetActive(true);
		}

		if(GameManager.Instance.player.hp)
		{
			GameManager.Instance.player.RestoreHP(GameManager.Instance.player.hp.max);
		}

		LivesManager.Instance.Show();
	}
}
