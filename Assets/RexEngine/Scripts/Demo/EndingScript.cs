/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RexEngine;

public class EndingScript:MonoBehaviour 
{
	public List<GameObject> paragraphs;

	protected int currentParagraph = 0;

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

		GameManager.Instance.player.gameObject.SetActive(false);
		GameManager.Instance.player.hp.bar.gameObject.SetActive(false);
		ScoreManager.Instance.text.gameObject.SetActive(false);
		PauseManager.Instance.isPauseEnabled = false;
		LivesManager.Instance.Hide();
	}
	
	void Update() 
	{
		if(GameManager.Instance.input.isJumpButtonDownThisFrame || Input.GetMouseButtonDown(0))
		{
			AdvanceText();
		}
	}

	protected void AdvanceText()
	{
		currentParagraph ++;
		if(currentParagraph <= paragraphs.Count - 1)
		{
			paragraphs[currentParagraph].SetActive(true);

			if(currentParagraph == paragraphs.Count - 1)
			{
				for(int i = 0; i < paragraphs.Count - 1; i ++)
				{
					paragraphs[i].gameObject.SetActive(false);
				}
			}
		}
		else
		{
			RexSceneManager.Instance.LoadSceneWithFadeOut("Demo_Title", Color.white);
		}
	}
}
