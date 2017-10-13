/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RexEngine;

public class GameOverScript:MonoBehaviour 
{
	public string sceneToExitTo = "Demo_Title"; //The scene to load after the player confirms the Game Over

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

		LivesManager.Instance.Hide();
		GameManager.Instance.player.gameObject.SetActive(false);
		GameManager.Instance.player.hp.bar.gameObject.SetActive(false);
		ScoreManager.Instance.text.gameObject.SetActive(false);
		PauseManager.Instance.isPauseEnabled = false;
	}

	void Update() 
	{
		if(GameManager.Instance.input.isJumpButtonDownThisFrame || Input.GetMouseButtonDown(0))
		{
			LoadTitle();
		}
	}

	protected void LoadTitle()
	{
		RexSceneManager.Instance.LoadSceneWithFadeOut(sceneToExitTo, Color.white);
	}
}
