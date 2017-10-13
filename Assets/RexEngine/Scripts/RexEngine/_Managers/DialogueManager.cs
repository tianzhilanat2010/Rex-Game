/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RexEngine
{
	public class DialogueManager:MonoBehaviour 
	{
		public TextMesh text;
		public AudioSource audio;
		public AudioClip showSound;
		public AudioClip hideSound;
		public GameObject advanceIcon;

		protected bool isCloseEnabled = false;

		private static DialogueManager instance = null;
		public static DialogueManager Instance 
		{ 
			get 
			{
				if(instance == null)
				{
					GameObject go = new GameObject();
					instance = go.AddComponent<DialogueManager>();
					go.name = "DialogueManager";
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
			Hide();
		}

		void Update()
		{
			if(text.gameObject.activeSelf)
			{
				if((GameManager.Instance.input.isJumpButtonDownThisFrame || Input.GetMouseButtonDown(0)) && isCloseEnabled)
				{
					Hide();
				}
			}
		}

		public void Show(string _text)
		{
			#if UNITY_ANDROID || UNITY_IPHONE
			RexTouchInput rexTouchInput = GameManager.Instance.player.GetComponent<RexTouchInput>();
			if(rexTouchInput != null)
			{
				rexTouchInput.ToggleTouchInterface(false);
			}
			#endif

			if(audio && showSound)
			{
				audio.PlayOneShot(showSound);
			}

			text.text = _text;
			text.gameObject.SetActive(true);
			GameManager.Instance.player.RemoveControl();
			isCloseEnabled = false;
			advanceIcon.SetActive(false);
			StartCoroutine("ShowCoroutine");
		}

		public void Hide()
		{
			#if UNITY_ANDROID || UNITY_IPHONE
			RexTouchInput rexTouchInput = GameManager.Instance.player.GetComponent<RexTouchInput>();
			if(rexTouchInput != null)
			{
				rexTouchInput.ToggleTouchInterface(true);
			}
			#endif

			if(audio && hideSound && text.gameObject.activeSelf)
			{
				audio.PlayOneShot(hideSound);
			}

			text.text = "";
			text.gameObject.SetActive(false);
			StartCoroutine("HideCoroutine");
		}

		protected IEnumerator ShowCoroutine()
		{
			yield return new WaitForSeconds(2.5f);

			advanceIcon.SetActive(true);
			isCloseEnabled = true;
		}

		protected IEnumerator HideCoroutine()
		{
			yield return new WaitForSeconds(0.1f);

			GameManager.Instance.player.RegainControl();
			isCloseEnabled = true;
		}
	}

}
