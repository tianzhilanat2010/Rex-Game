﻿/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RexEngine
{
	[SelectionBase]
	public class RexActor:RexObject
	{
		[System.Serializable]
		public class DamagedControllers
		{
			public RexController regularController;
			public RexController damagedController;
			public int hpThreshold = 1; //If both RexControllers are slotted, this will change to damagedController when hp is equal to or less than hpThreshold, and back to regularController if hp is greater than hpThreshold
		}

		[System.Serializable]
		public class DamagedProperties
		{
			public bool willFlash = true; //If the actor flashes white when they take damage
			public bool willShakeScreen; //If the screen shakes when the actor takes damage
			public bool willShowDamagedNumber = false; //If True, this will display numbers showing the amount it was damaged for when it's damaged
			public List<SpriteRenderer> spritesToFlash; //A slottable list of all the child SpriteRenderers that should flash when the actor takes damage
			public DamagedControllers damagedControllers;
		}

		[System.Serializable]
		public class DeathProperties
		{
			public bool willShakeScreen = true;
			public RexParticle deathParticle;
			public DestroyOnDeath destroyOnDeath;
			public float durationBeforeClear;
		}

		[System.Serializable]
		public class Invincibility
		{
			public bool isAlwaysInvincible = false; //If True, this actor is always invincible and will never take damage
			public bool willBlinkDuringDamagedInvincibility; //if the actor's sprite blinks while it's invincible after taking damage
			public float damagedInvincibilityDuration = 0.15f; //The duration of invincibility after the actor is damaged

			[HideInInspector]
			public bool isDamagedInvincibilityActive = false; //If the actor is currently invincible from taking damage

			[HideInInspector]
			public bool isPowerupInvincibilityActive = false; //If the actor is currently invincible due to a powerup

			[HideInInspector]
			public bool isDebugInvincibilityActive = false; //If the actor is invincible because of debug settings (Tools > RexEngine> EnableDebugInvincibility)
		}

		[System.Serializable]
		public class WaterProperties
		{
			public RexController landController; //The RexController used while the actor is on land
			public RexController waterController; //The RexController used while the actor is in water
			public bool willChangeControllerInWater; //If True, the actor will use landController on land and waterController in water, and auto-change between them when appropriate

			[HideInInspector]
			public bool isInWater = false; //If the actor is currently in water

			[HideInInspector]
			public int waterBodiesTouched; //The number of water bodies the actor is currently touching; used to calculate isInWater
		}

		[HideInInspector]
		public Attack currentAttack; //The attack, if any, that the actor is currently performing

		[HideInInspector]
		public bool isBeingLoadedIntoNewScene = false; //Used to temporarily freeze position while a new scene is loading

		[HideInInspector]
		public bool isDead; //True while the actor is dead, and thus can't move or be interacted with

		public WaterProperties waterProperties;

		public Invincibility invincibility;
		public DamagedProperties damagedProperties;
		public DeathProperties deathProperties;

		public Energy hp; //Slot an Energy component here for the actor's HP, or Hit Points
		public Energy mp; //Slot an Energy component here for the actor's MP, or Magic Points

		public bool canBounceOn = true; //If another actor's Controller has a Bounce component, setting this to True allows them to bounce on this

		[System.Serializable]
		public enum DestroyOnDeath
		{
			AfterDeathAnimation,
			AfterDuration,
			Never
		}

		[System.NonSerialized]
		public Vector3 loadedIntoScenePoint; //Used by RexSceneManager to know where to load the actor when a new room is entered

		protected Material flashMaterial; //The material used when the actor flashes when damaged
		protected Material defaultMaterial; //The initial starting material used by the main SpriteRenderer on the actor
		protected bool isFlashing; //Whether or not the actor is currently flashing after being damaged
		protected bool willReparentToActorsObject = true; //If the actor will auto-parent to the Actors GameObject on Awake; used for organization in scenes

		void Awake()
		{
			SetFlashMeterial();
		}

		#region public methods
		//Makes the actor briefly flash white; most commonly used when the actor is damaged
		public void Flash()
		{
			if(flashMaterial == null)
			{
				SetFlashMeterial();
			}

			if(!isFlashing)
			{
				StopCoroutine("FlashCoroutine");
				StartCoroutine("FlashCoroutine");
			}
		}

		//Makes the sprite blink on and off
		public void Blink()
		{
			StartCoroutine("BlinkCoroutine");
		}

		public void StopBlink()
		{
			StopCoroutine("BlinkRoutine");

			int len = damagedProperties.spritesToFlash.Count;
			for(int i = 0; i < len; i ++)
			{
				damagedProperties.spritesToFlash[i].enabled = true;
			}
		}

		//Called automatically by a body of water when it comes into contact with a player
		public virtual void NotifyOfWaterlineContact(CollisionType collisionType)
		{
			if(collisionType == CollisionType.Exit)
			{
				waterProperties.waterBodiesTouched --;
				if(waterProperties.waterBodiesTouched <= 0)
				{
					waterProperties.waterBodiesTouched = 0;
					OnExitWater();

					if(waterProperties.willChangeControllerInWater && waterProperties.landController)
					{
						string previousStateID = slots.controller.StateID();
						SetController(waterProperties.landController);
						slots.controller.GetComponent<JumpState>().ForceBegin();
					}
				}
			}
			else
			{
				waterProperties.waterBodiesTouched ++;
				OnEnterWater();

				if(waterProperties.willChangeControllerInWater && waterProperties.waterController)
				{
					SetController(waterProperties.waterController);
				}
			}

			if(waterProperties.waterBodiesTouched > 0)
			{
				waterProperties.isInWater = true;
			}
			else
			{
				waterProperties.isInWater = false;
			}
		}

		//Swaps out the current RexController for another; most commonly used if the actor has multiple RexControllers, i.e. one for land, one for water, one for air, etc.
		public void SetController(RexController _controller)
		{
			if(slots.controller)
			{
				slots.controller.EndAllStates();
				slots.controller.isEnabled = false;
				slots.controller.gameObject.SetActive(false);
				slots.controller.CancelTurn();
				slots.controller.StopAllCoroutines();
				slots.controller.SetAxis(new Vector2(0.0f, 0.0f));
			}

			_controller.gameObject.SetActive(true);
			_controller.isEnabled = true;
			_controller.SetStateToDefault(true);
			_controller.CancelTurn();
			_controller.direction = slots.controller.direction;
			_controller.AnimateEnable();
			_controller.SetAxis(new Vector2(0.0f, 0.0f));
			_controller.direction = slots.controller.direction;

			slots.controller = _controller;

			BoxCollider2D controllerCollider = slots.controller.GetComponent<BoxCollider2D>();
			if(controllerCollider != null && slots.collider != null)
			{
				BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
				if(boxCollider != null)
				{
					float originalColliderSizeY = boxCollider.size.y;
					boxCollider.size = controllerCollider.bounds.size;	
					float difference = originalColliderSizeY - controllerCollider.bounds.size.y;

					SetPosition(new Vector2(transform.position.x, transform.position.y - difference * 0.5f));
				}
			}

			if(slots.physicsObject)
			{
				if(_controller.overrideMaxFallSpeed != 0.0f)
				{
					slots.physicsObject.gravitySettings.maxFallSpeed = _controller.overrideMaxFallSpeed;
				}
			}

			OnControllerChanged(_controller);
		}

		//Called by an Attack to notify the actor that the Attack is started
		public virtual void OnAttackStarted(Attack attack = null)
		{

		}

		//Called by an Attack to notify the actor that the Attack is completed
		public virtual void OnAttackComplete()
		{
			if(slots.controller)
			{
				slots.controller.OnAttackComplete();
			}
			/*if(slots.controller && slots.controller.currentState)
			{
				slots.controller.currentState.PlayAnimation();
			}*/
		}

		//Used to remove player control from the actor
		public void RemoveControl()
		{
			if(slots.input)
			{
				slots.input.isEnabled = false;
			}
		}

		//Used to give control back to the actor if it's been removed
		public void RegainControl()
		{
			if(slots.input)
			{
				slots.input.isEnabled = true;
			}
		}

		public bool IsAttacking()
		{
			return (currentAttack != null && currentAttack.isAttackActive);
		}

		//If an HP Energy component is slotted, add to the current value
		public void RestoreHP(int amount)
		{
			if(hp != null)
			{
				hp.Restore(amount);

				if(damagedProperties.damagedControllers.damagedController && hp.current > damagedProperties.damagedControllers.hpThreshold)
				{
					SetController(damagedProperties.damagedControllers.regularController);
				}
			}
		}

		//If an MP Energy component is slotted, add to the current value
		public void RestoreMP(int amount)
		{
			if(mp != null)
			{
				mp.Restore(amount);
			}
		}

		//If an MP Energy component is slotted, subtract from the current value
		public void DecrementMP(int amount)
		{
			if(mp != null)
			{
				mp.Decrement(amount);
			}
		}

		//If an HP Energy component is attached, this lowers its value by the amount damaged. Also does processing to ensure the player CAN be damaged first (i.e. that it isn't currently invincible) and determines if knockback should be applied.
		public int Damage(int amount, bool willCauseKnockback = true, BattleEnums.DamageType damageType = BattleEnums.DamageType.Regular, Collider2D col = null) 
		{
			int damageTaken = 0;

			if(!WillProcessDamageFromObject(col) || DetermineIfInvincible() || isDead)
			{
				return 0;
			}

			if(hp != null)
			{
				hp.Decrement(amount);
				damageTaken = hp.previous - hp.current;
			}

			if(damagedProperties.willShowDamagedNumber)
			{
				float yPosition = (slots.collider != null) ? slots.collider.bounds.max.y : transform.position.y; 
				DamageNumberManager.Instance.ShowNumber(amount, new Vector2(transform.position.x, yPosition));
			}

			Direction.Horizontal knockbackDirection = Direction.Horizontal.Right;

			if(damageType != BattleEnums.DamageType.Poison)
			{
				if(damagedProperties.willFlash)
				{
					Flash();
				}
			}

			if(hp == null)
			{
				return 0;
			}

			if(hp.current <= 0)
			{
				SetDeathParameters();
			}
			else
			{
				if(damageType != BattleEnums.DamageType.Poison)
				{
					if(damagedProperties.willShakeScreen)
					{
						ScreenShake.Instance.Shake();
					}

					if(willCauseKnockback)
					{
						if(col != null)
						{
							knockbackDirection = (col.transform.position.x < transform.position.x) ? Direction.Horizontal.Right : Direction.Horizontal.Left;
						}
						else
						{
							knockbackDirection = (slots.controller && slots.controller.direction.horizontal == Direction.Horizontal.Left) ? Direction.Horizontal.Right : Direction.Horizontal.Left;
						}

						if(currentAttack != null)
						{
							if(currentAttack.canceledBy.onKnockback)
							{
								currentAttack.Cancel();
							}
							else
							{
								willCauseKnockback = false;
							}
						}
					}

					bool didChangeController = false;
					if(damagedProperties.damagedControllers.damagedController && hp.current <= damagedProperties.damagedControllers.hpThreshold)
					{
						SetController(damagedProperties.damagedControllers.damagedController);
						didChangeController = true;
					}

					StartDamageInvincibility();

					if(!(currentAttack != null && !currentAttack.canceledBy.onKnockback) && willCauseKnockback)
					{
						if(slots.controller && !didChangeController)
						{
							slots.controller.Knockback(knockbackDirection);
						}
					}
				}
			}

			if(col != null && damageType != BattleEnums.DamageType.Poison)
			{
				HitSparkManager.Instance.CreateHitSpark(GetComponent<BoxCollider2D>(), col.GetComponent<BoxCollider2D>(), (damageTaken > 0), hp.current);
				OnHit(damageTaken, col);
				if(col.GetComponent<RexActor>() != null)
				{
					//col.GetComponent<RexActor>().StartContactDelay();
				}

				if(GetComponent<Jitter>())
				{
					GetComponent<Jitter>().Play();
				}
			}

			return damageTaken;
		}

		public void Revive() //Revives the actor from the Death state
		{
			isDead = false;

			if(slots.controller)
			{
				slots.controller.SetToAlive();

				if(hp != null)
				{
					hp.SetToMax();
				}
			}

			if(GetComponent<BoxCollider2D>())
			{
				GetComponent<BoxCollider2D>().enabled = true;
			}

			if(slots.physicsObject)
			{
				slots.physicsObject.isEnabled = true;
			}

			OnRevive();
		}

		//Immediately sets HP to 0 (if an HP component is slotted) and kills the actor
		public void KillImmediately()
		{
			if(hp != null)
			{
				hp.Decrement(hp.current);
			}

			SetDeathParameters();
		}

		//Called whenever the controller changes state. This can be overidden to have unique secondary effects when an actor changes to a particular state.
		public virtual void OnStateChanged(RexState  newState){}

		//Called whenever the controller changes state. This can be overidden to have unique secondary effects when an actor changes to a particular state.
		public virtual void OnStateEntered(RexState  newState){}

		//Called whenever the controller changes state. This can be overidden to have unique secondary effects when an actor changes to a particular state.
		public virtual void OnStateExited(RexState  newState){}

		public virtual void NotifyOfControllerJumping(int jumpNumber){}

		public virtual void NotifyOfControllerJumpCresting(){}

		public virtual void Reset(){}

		#endregion

		#region private methods
		//Sets all the parameters required to treat the actor as "Dead"; should be called via KillImmediately
		protected void SetDeathParameters()
		{
			CancelInvoke();
			canBounceOn = false;

			if(currentAttack != null)
			{
				currentAttack.Cancel();
			}

			if(slots.controller)
			{
				slots.controller.SetToDead();
			}

			if(slots.physicsObject)
			{
				slots.physicsObject.isEnabled = false;
			}

			if(deathProperties.willShakeScreen)
			{
				ScreenShake.Instance.Shake();
			}

			if(deathProperties.deathParticle)
			{
				deathProperties.deathParticle.gameObject.SetActive(true);
				deathProperties.deathParticle.Play();
			}

			OnDeath();

			if(deathProperties.destroyOnDeath != DestroyOnDeath.Never)
			{
				StartCoroutine("ClearCoroutine");
				StartCoroutine("DisableColliderCoroutine");
			}

			isDead = true;
		}

		protected IEnumerator DisableColliderCoroutine()
		{
			yield return new WaitForEndOfFrame();

			if(GetComponent<BoxCollider2D>())
			{
				GetComponent<BoxCollider2D>().enabled = false;
			}
		}

		protected IEnumerator ClearCoroutine()
		{
			yield return new WaitForEndOfFrame();

			float durationBeforeDestroy = deathProperties.durationBeforeClear;
			if(deathProperties.destroyOnDeath == DestroyOnDeath.AfterDeathAnimation && slots.controller && slots.controller.animations.deathAnimation)
			{
				durationBeforeDestroy = slots.controller.animations.deathAnimation.length;
			}

			yield return new WaitForSeconds(durationBeforeDestroy);

			Clear();
		}

		//Can be overidden to determine whether the actor will be damaged or not by specific things
		protected virtual bool WillProcessDamageFromObject(Collider2D col)
		{
			return true;
		}

		//Loads the proper material to make the actor flash on damaged
		protected virtual void SetFlashMeterial()
		{
			flashMaterial = Resources.Load("Materials/flash", typeof(Material)) as Material;
			if(slots.spriteRenderer != null)
			{
				defaultMaterial = slots.spriteRenderer.material;
			}
		}

		//Locks the actor into the SceneBoundaries
		protected void EnableCollisionWithSceneBoundaries()
		{
			slots.physicsObject.AddToCollisions("Boundaries");
			willDespawnOnSceneExit = false;
		}

		//Returns True if the actor is invincible for any reason
		protected bool DetermineIfInvincible()
		{
			return (invincibility.isAlwaysInvincible || invincibility.isDamagedInvincibilityActive || invincibility.isDebugInvincibilityActive || invincibility.isPowerupInvincibilityActive);
		}

		//Ends the flash
		protected void EndFlash()
		{
			if(defaultMaterial != null)
			{
				int len = damagedProperties.spritesToFlash.Count;
				for(int i = 0; i < len; i++)
				{
					damagedProperties.spritesToFlash[i].material = defaultMaterial as Material;
				}
			}

			isFlashing = false;
		}

		//Governs the sequence where the player flashes and calls EndFlash after Flash is called; should be called from Flash()
		protected IEnumerator FlashCoroutine()
		{
			float duration = 0.175f;
			isFlashing = true;

			int len = damagedProperties.spritesToFlash.Count;
			for(int i = 0; i < len; i++)
			{
				damagedProperties.spritesToFlash[i].material = flashMaterial;
			}

			yield return new WaitForSeconds(duration);

			EndFlash();
		}

		protected virtual IEnumerator BlinkCoroutine()
		{
			float delay = 0.1f;

			int len = damagedProperties.spritesToFlash.Count;
			while(invincibility.isDamagedInvincibilityActive)
			{
				for(int i = 0; i < len; i ++)
				{
					damagedProperties.spritesToFlash[i].enabled = false;
				}

				yield return new WaitForSeconds(delay);

				for(int i = 0; i < len; i ++)
				{
					damagedProperties.spritesToFlash[i].enabled = true;
				}

				yield return new WaitForSeconds(delay);
			}

			StopBlink();
		}

		//Begins the Damaged invincibility that plays where the actor is briefly invincible after taking damage
		protected void StartDamageInvincibility()
		{
			StartCoroutine("DamageInvincibilityCoroutine");
		}

		protected IEnumerator DamageInvincibilityCoroutine()
		{
			invincibility.isDamagedInvincibilityActive = true;

			if(invincibility.willBlinkDuringDamagedInvincibility)
			{
				Blink();
			}

			if(invincibility.damagedInvincibilityDuration > 0.0f)
			{
				yield return new WaitForSeconds(invincibility.damagedInvincibilityDuration);
			}
			else
			{
				yield return new WaitForEndOfFrame();
			}

			if(slots.collider) //Disabling and re-enabling the collider allows OnTriggerEnter2D to be called again
			{
				slots.collider.enabled = false;
				slots.collider.enabled = true;
			}

			invincibility.isDamagedInvincibilityActive = false;
		}

		protected virtual void ProcessCollision(Collider2D col, CollisionType type = CollisionType.Enter)
		{
			if(col.tag != "Terrain") //Ignoring terrain collisions speeds up performance here
			{
				bool willBounce = false;
				if(type == CollisionType.Enter)
				{
					if(slots.controller)
					{
						BounceState bounce = slots.controller.bounceState;
						if(bounce && slots.collider)
						{
							if(bounce.CanBounce(slots.collider, col))
							{
								willBounce = true;
								bounce.StartBounce(slots.collider, col);
								RexActor bouncedOnActor = col.GetComponent<RexActor>();
								if(bouncedOnActor)
								{
									bouncedOnActor.Damage(bounce.damageDealt, false);
									bouncedOnActor.OnBouncedOn(slots.collider);
								}
							}
						}
					}
				}

				if(!willBounce)
				{
					ContactDamage contactDamage = col.GetComponent<ContactDamage>();
					if(contactDamage != null)
					{
						if(col.GetComponent<RexActor>() && col.GetComponent<RexActor>().isDead)
						{
							return;
						}

						if(tag == "Player" && contactDamage.willDamagePlayer)
						{
							Damage(contactDamage.amount, true, BattleEnums.DamageType.Regular, col);
						}
						else if(tag == "Enemy" && contactDamage.willDamageEnemies)
						{
							Damage(contactDamage.amount, true, BattleEnums.DamageType.Regular, col);
						}
					}
				}
			}
		}

		protected virtual void OnDeath() //This can be overidden by the actor to have unique effects when the actor dies
		{
			if(gameObject.tag == "Player")
			{
				GameManager.Instance.OnPlayerDeath();
			}
		} 

		protected virtual void OnRevive() //This can be overidden by the actor to have unique effects when the actor is revived
		{
			if(gameObject.tag == "Player")
			{
				if(slots.spriteHolder)
				{
					slots.spriteHolder.gameObject.SetActive(true);
				}

				slots.physicsObject.AnchorToFloor();
			}
		} 

		protected virtual void OnControllerChanged(RexController _newController){} //This can be used to trigger specific effects when the controller changes

		//Can be overidden to have secondary effects play when the actor enters the water
		protected virtual void OnEnterWater(){}

		//Can be overidden to have secondary effects play when the actor exits the water
		protected virtual void OnExitWater(){}

		//Can be overidden to have secondary effects play when the actor is hit
		protected virtual void OnHit(int damageTaken, Collider2D col = null){}

		//Can be overriden to have unique secondary effects when another actor bounces on this
		protected virtual void OnBouncedOn(Collider2D col = null){}

		#endregion

		#region footer

		protected void OnTriggerEnter2D(Collider2D col)
		{
			ProcessCollision(col, CollisionType.Enter);
		}

		/*protected void OnTriggerStay2D(Collider2D col)
		{
			ProcessCollision(col, CollisionType.Stay);
		}*/

		void OnDestroy()
		{
			flashMaterial = null;
			slots.spriteRenderer = null;
		}

		#endregion
	}
}
