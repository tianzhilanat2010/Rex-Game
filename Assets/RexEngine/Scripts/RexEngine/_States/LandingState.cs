/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class LandingState:RexState
	{
		public float fallDistanceForStun = 0.0f; //If the actor falls a greater distance than this, they'll be stunned for the duration of the landing animation. Setting this to 0.0f disables the stun altogether.
		public AnimationClip stunnedAnimation;
		public RexPool landingParticlePool; //If slotted, this will play when the actor lands on a surface
		public Vector2 particleOffset;

		public const string idString = "Landing";

		protected bool willStun;

		void Awake() 
		{
			id = idString;
			willPlayAnimationOnBegin = false;
			GetController();
		}

		#region unique public methods

		public void CheckStun(float distanceFallen)
		{
			willStun = (Mathf.Abs(fallDistanceForStun) > 0.0f && distanceFallen >= fallDistanceForStun);
		}

		#endregion

		#region override public methods

		public override void OnBegin()
		{
			if(willStun)
			{
				controller.Stun();
			}

			StartCoroutine("LandingCoroutine");
		}

		public override void OnEnded()
		{
			StopCoroutine("LandingCoroutine");
			//controller.isStunned = false;
			controller.SetStateToDefault();
		}

		public override void OnStateChanged()
		{
			StopCoroutine("LandingCoroutine");
		}

		#endregion

		protected virtual IEnumerator LandingCoroutine()
		{
			AnimationClip animationClip = (willStun && stunnedAnimation != null) ? stunnedAnimation : animation;
			float duration = (animationClip != null ) ? animationClip.length : 0.0f;

			if(willStun && stunnedAnimation != null)
			{
				PlaySecondaryAnimation(animationClip);
			}
			else
			{
				PlayAnimation();
			}

			if(landingParticlePool)
			{
				SpawnLandingParticle();
			}

			yield return new WaitForSeconds(duration);

			End();
		}

		protected void SpawnLandingParticle()
		{
			GameObject particle = landingParticlePool.Spawn();
			ParentHelper.Parent(particle, ParentHelper.ParentObject.Particles);
			particle.transform.position = new Vector3(landingParticlePool.transform.position.x + particleOffset.x, landingParticlePool.transform.position.y + particleOffset.y, 0.0f);
			particle.GetComponent<RexParticle>().Play();
		}
	}
}
