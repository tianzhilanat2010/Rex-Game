using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class LivesManager:MonoBehaviour 
	{
		public int startingLives = 3;
		public int maxLives = 99;
		public bool areLivesEnabled = true;
		public TextMesh textMesh;
		public GameObject icon;

		protected int currentLives;

		private static LivesManager instance = null;
		public static LivesManager Instance 
		{ 
			get 
			{
				if(instance == null)
				{
					GameObject go = new GameObject();
					instance = go.AddComponent<LivesManager>();
					go.name = "LivesManager";
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
		}

		void Start() 
		{
			currentLives = startingLives;
			UpdateTextMesh();

			if(!areLivesEnabled)
			{
				Hide();
				gameObject.SetActive(false);
			}
		}

		public void IncrementLives(int amountToIncrement = 1)
		{
			if(areLivesEnabled)
			{
				currentLives ++;
				if(currentLives > maxLives)
				{
					currentLives = maxLives;
				}

				UpdateTextMesh();
			}
		}

		public bool DecrementLives(int amountToDecrement = 1)
		{
			bool isGameOver = false;

			if(areLivesEnabled)
			{
				currentLives -= amountToDecrement;
				if(currentLives < 0)
				{
					currentLives = 0;
					isGameOver = true;
				}

				UpdateTextMesh();
			}

			return isGameOver;
		}

		public void ResetLivesToStartingValue()
		{
			currentLives = startingLives;
			UpdateTextMesh();
		}

		public void Show()
		{
			if(areLivesEnabled)
			{
				textMesh.gameObject.SetActive(true);
				icon.SetActive(true);
			}
		}

		public void Hide()
		{
			textMesh.gameObject.SetActive(false);
			icon.SetActive(false);
		}

		protected void UpdateTextMesh()
		{
			if(textMesh != null)
			{
				textMesh.text = currentLives.ToString();
			}
		}
	}
}
