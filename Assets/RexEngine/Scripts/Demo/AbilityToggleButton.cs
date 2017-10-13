/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class AbilityToggleButton:RexActor 
	{
		public enum MechanicType
		{
			Weapon,
			FixedJump,
			Acceleration
		}

		public enum WeaponType
		{
			Melee,
			Projectile
		}

		public MechanicType mechanicType;
		public Sprite pressedSprite;
		public Sprite originalSprite;
		public AudioSource audio;
		public AudioClip pressSound;

		protected bool hasActivated;
		protected WeaponType currentWeaponType;
		protected bool isJumpFixed;
		protected bool isAccelerationEnabled;

		void Awake() 
		{

		}

		protected void AddMechanic()
		{
			Debug.Log("Adding: " + mechanicType);
			switch(mechanicType)
			{
				case MechanicType.Weapon:
					ToggleWeapon();
					break;
				case MechanicType.FixedJump:
					ToggleFixedJump();
					break;
				case MechanicType.Acceleration:
					ToggleAcceleration();
					break;
				default:
					AddBounce();
					break;
			}
		}

		protected void ToggleWeapon()
		{
			if(currentWeaponType == WeaponType.Melee)
			{
				currentWeaponType = WeaponType.Projectile;
				GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Charge").GetComponent<Attack>().isEnabled = true;
				GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Charge").GetComponent<Attack>().attackInputImportance = Attack.AttackImportance.Primary;
				GameManager.Instance.player.transform.Find("Attacks").Find("Melee").GetComponent<Attack>().isEnabled = false;
			}
			else if(currentWeaponType == WeaponType.Projectile)
			{
				currentWeaponType = WeaponType.Melee;
				GameManager.Instance.player.transform.Find("Attacks").Find("Melee").GetComponent<Attack>().isEnabled = true;
				GameManager.Instance.player.transform.Find("Attacks").Find("Melee").GetComponent<Attack>().attackInputImportance = Attack.AttackImportance.Primary;
				GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Charge").GetComponent<Attack>().isEnabled = false;
			}
		}

		protected void ToggleFixedJump()
		{
			if(!isJumpFixed)
			{
				isJumpFixed = true;
				GameManager.Instance.player.slots.controller.GetComponent<JumpState>().freezeHorizontalMovement = true;
			}
			else if(isJumpFixed)
			{
				isJumpFixed = false;
				GameManager.Instance.player.slots.controller.GetComponent<JumpState>().freezeHorizontalMovement = false;
			}
		}

		protected void ToggleAcceleration()
		{
			if(!isAccelerationEnabled)
			{
				isAccelerationEnabled = true;
				GameManager.Instance.player.slots.controller.GetComponent<MovingState>().movementProperties.acceleration = 0.25f;
				GameManager.Instance.player.slots.controller.GetComponent<MovingState>().movementProperties.deceleration = 0.25f;
			}
			else if(isAccelerationEnabled)
			{
				isAccelerationEnabled = false;
				GameManager.Instance.player.slots.controller.GetComponent<MovingState>().movementProperties.acceleration = 0.0f;
				GameManager.Instance.player.slots.controller.GetComponent<MovingState>().movementProperties.deceleration = 0.0f;
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

		protected void AddFlying()
		{
			DataManager.Instance.hasUnlockedFly = true;
			StartCoroutine("AddFlyingCoroutine");
		}

		//This is to let the player finish out their bounce before gravity is disabled; it just feels good
		protected IEnumerator AddFlyingCoroutine()
		{
			GameManager.Instance.player.slots.controller.GetComponent<JumpState>().type = JumpState.JumpType.None;
			GameManager.Instance.player.slots.controller.GetComponent<MovingState>().canMoveVertically = true;

			GameManager.Instance.player.transform.Find("Attacks").Find("Melee").GetComponent<Attack>().isEnabled = false;
			GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Charge").GetComponent<Attack>().isEnabled = true;
			GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Charge").GetComponent<Attack>().attackInputImportance = Attack.AttackImportance.Primary;

			yield return new WaitForSeconds(0.35f);

			GameManager.Instance.player.slots.physicsObject.gravitySettings.usesGravity = false;
			GameManager.Instance.player.slots.controller.GetComponent<BounceState>().isEnabled = false;
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

				ScreenShake.Instance.Shake();
				AddMechanic();
				StartCoroutine("DisplayTextCoroutine");
				StartCoroutine("BounceCoroutine");
			}
		}

		protected IEnumerator BounceCoroutine()
		{
			slots.spriteRenderer.sprite = pressedSprite;
			hasActivated = true;
			GetComponent<BoxCollider2D>().enabled = false;
			yield return new WaitForSeconds(2.5f);

			hasActivated = false;
			GetComponent<BoxCollider2D>().enabled = true;
			slots.spriteRenderer.sprite = originalSprite;
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
				case MechanicType.Weapon:
					string weaponName = (currentWeaponType == WeaponType.Melee) ? "Wrench Slash" : "Pistol";
					description = "Weapon changed to: " + weaponName + "!";
					break;
				case MechanicType.FixedJump:
					description = (isJumpFixed) ? "Jump is now horizontally fixed!" : "Jump is no longer horizontally fixed!";
					break;
				case MechanicType.Acceleration:
					description = (isAccelerationEnabled) ? "Movement now has acceleration and deceleration!" : "Movement no longer has acceleration or \ndeceleration!";
					break;
				/*case MechanicType.Flying:
					description = "A double-jump isn't enough to get up \nthis shaft, but with RexEngine, we can \nmake ourselves fly! Now you can \nmove up and down in midair!";
					break;*/
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
