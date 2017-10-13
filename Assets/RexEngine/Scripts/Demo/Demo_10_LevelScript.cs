/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RexEngine;

public class Demo_10_LevelScript:MonoBehaviour 
{
	public TextMesh victoryText;
	public TextMesh scoreText;
	public TextMesh deathsText;

	void Awake() 
	{
		
	}

	void Start() 
	{
		
	}
	
	void Update() 
	{
		
	}

	public void RunEnding()
	{
		Debug.Log("End!");
		StartCoroutine("EndingCoroutine");
	}

	protected IEnumerator EndingCoroutine()
	{
		RexSoundManager.Instance.Fade();
		GameManager.Instance.player.slots.input.isEnabled = false;
		GameManager.Instance.player.hp.bar.gameObject.SetActive(false);
		ScoreManager.Instance.text.gameObject.SetActive(false);
		PauseManager.Instance.isPauseEnabled = false;

		Debug.Log("BLUEPRINT RECOVERED!");
		ScreenShake.Instance.Shake(); 
		victoryText.gameObject.SetActive(true);
		GameManager.Instance.player.GetComponent<Booster>().OnBlueprintFound();
		yield return new WaitForSeconds(2.0f);

		string scoreString = "Total bolts collected: " + ScoreManager.Instance.score.ToString();
		scoreText.text = scoreString;
		scoreText.gameObject.SetActive(true);
		yield return new WaitForSeconds(1.0f);

		string deathString = "Total deaths: " + DataManager.Instance.deaths.ToString();
		deathsText.text = deathString;
		deathsText.gameObject.SetActive(true);

		yield return new WaitForSeconds(5.0f);

		RexSceneManager.Instance.LoadSceneWithFadeOut("Demo_Ending");
	}
}
