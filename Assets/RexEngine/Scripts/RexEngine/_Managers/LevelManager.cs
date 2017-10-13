/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RexEngine
{
	public class LevelManager:MonoBehaviour 
	{
		public bool isPauseEnabledInScene = true;
		public AudioClip musicTrack;

		void Start() 
		{
			OnLevelStart();
		}

		protected void OnLevelStart()
		{
			if(ScoreManager.Instance.text)
			{
				ScoreManager.Instance.text.gameObject.SetActive(true);
			}

			PauseManager.Instance.isPauseEnabled = isPauseEnabledInScene;
			RexSoundManager.Instance.SetMusic(musicTrack);
		}
	}
}
