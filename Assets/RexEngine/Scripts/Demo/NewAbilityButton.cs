/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class NewAbilityButton:RexActor 
	{
		public enum MechanicType
		{
			Bounce,
			Projectile,
			DoubleJump,
			Flying,
			WallCling
		}

		public MechanicType mechanicType;
		public Sprite pressedSprite;
		public AudioSource audio;
		public AudioClip pressSound;

		protected bool hasActivated;

		void Awake() 
		{

		}

		protected void AddMechanic()
		{
			Debug.Log("Adding: " + mechanicType);
			switch(mechanicType)
			{
				case MechanicType.Bounce:
					AddBounce();
					break;
				case MechanicType.Projectile:
					AddProjectileAttack();
					break;
				case MechanicType.DoubleJump:
					AddDoubleJump();
					break;
				case MechanicType.Flying:
					AddFlying();
					break;
				case MechanicType.WallCling:
					AddWallCling();
					break;
				default:
					AddBounce();
					break;
			}
		}

		protected void AddDoubleJump()
		{
			DataManager.Instance.hasUnlockedDoubleJump = true;
			GameManager.Instance.player.slots.controller.GetComponent<JumpState>().multipleJumpNumber = 2;
		}

		protected void AddProjectileAttack()
		{
			DataManager.Instance.hasUnlockedProjectile = true;
			GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Charge").GetComponent<Attack>().isEnabled = true;
		}

		protected void AddWallCling()
		{
			DataManager.Instance.hasUnlockedWallCling = true;
			GameManager.Instance.player.slots.controller.GetComponent<WallClingState>().isEnabled = true;
		}

		protected void AddFlying()
		{
			DataManager.Instance.hasUnlockedFly = true;
			StartCoroutine("AddFlyingCoroutine");
		}

		//This is to let the player finish out their bounce before gravity is disabled; it just feels good
		protected IEnumerator AddFlyingCoroutine()
		{
			yield return new WaitForSeconds(0.525f);

			GameManager.Instance.player.GetComponent<Booster>().SetToFlyingController();
			GameManager.Instance.player.slots.physicsObject.gravitySettings.usesGravity = false;
			ScreenShake.Instance.Shake();
		}

		protected void AddBounce()
		{
			DataManager.Instance.hasUnlockedBounce = true;
			GameManager.Instance.player.slots.controller.GetComponent<BounceState>().isEnabled = true;
		}

		protected override void OnBouncedOn(Collider2D col = null)
		{
			if(!hasActivated)
			{
				if(audio && pressSound)
				{
					audio.PlayOneShot(pressSound);
				}

				hasActivated = true;
				ScreenShake.Instance.Shake();
				AddMechanic();
				GetComponent<BoxCollider2D>().enabled = false;
				StartCoroutine("DisplayTextCoroutine");
				slots.spriteRenderer.sprite = pressedSprite;
			}
		}

		protected IEnumerator DisplayTextCoroutine()
		{
			yield return new WaitForSeconds(0.5f);

			string abilityDescription = GetAbilityDescription();
			DialogueManager.Instance.Show(abilityDescription);
		}

		protected string GetAbilityDescription()
		{
			string description = "";
			switch(mechanicType)
			{
				case MechanicType.Bounce:
					description = "Rex Engine makes it easy to give yourself \nnew powers with the press of a button! \nNow you can bounce on top of enemies! \nBam!";
					break;
				case MechanicType.Projectile:
					description = "Now you can fire bullets by pressing 'T'! \n\nYou can also fire charged shots\n by holding T for several seconds\n before releasing it!";
					break;
				case MechanicType.DoubleJump:
					description = "Now you can double-jump by pressing \nthe Jump key again in midair!";
					break;
				case MechanicType.Flying:
					description = "A double-jump isn't enough to get up \nthis shaft, but with Rex Engine, we can \nmake ourselves fly! Now you can \nmove up and down in midair!";
					break;
				case MechanicType.WallCling:
					description = "Now you can cling to walls and wall-jump!\n There are even options to climb up\n and down walls and hang from ledges!";
					break;
				default:
					description = "Rex Engine makes it easy to give yourself new powers with the press of a button! Now you can bounce on top of enemies! Bam!";
					break;
			}

			return description;
		}

		protected void OnTriggerEnter2D(Collider2D col)
		{
			if(col.tag == "Player")
			{
				GameManager.Instance.player.slots.controller.GetComponent<BounceState>().isEnabled = true;
			}
		}
	}
}
