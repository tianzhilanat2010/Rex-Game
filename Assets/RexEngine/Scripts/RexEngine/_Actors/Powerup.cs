/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RexEngine
{
	public class Powerup:RexObject
	{
		[System.Serializable]
		public class Sounds
		{
			public AudioClip collectSound;
		}

		public RexParticle collectParticle;

		public enum Equation
		{
			Increment,
			Decrement
		}

		public Sounds sounds;

		void Awake()
		{

		}

		void Start() 
		{

		}

		protected virtual void Kill()
		{
			GetComponent<Collider2D>().enabled = false;
			slots.spriteRenderer.enabled = false;
			if(sounds.collectSound != null)
			{
				PlaySoundIfOnCamera(sounds.collectSound);
			}

			if(collectParticle)
			{
				collectParticle.Play();
			}
		}

		protected virtual void TriggerEffect(RexActor player)
		{

		}

		protected IEnumerator KillCoroutine()
		{
			Kill();
			yield return new WaitForSeconds(1.5f);

			Destroy(gameObject);
		}

		protected void ProcessCollision(Collider2D col)
		{
			if(col.gameObject.tag == "Player")
			{
				TriggerEffect(col.gameObject.GetComponent<RexActor>());
				StartCoroutine(KillCoroutine());
			}
		}

		protected void OnTriggerEnter2D(Collider2D col)
		{
			ProcessCollision(col);
		}
	}

}