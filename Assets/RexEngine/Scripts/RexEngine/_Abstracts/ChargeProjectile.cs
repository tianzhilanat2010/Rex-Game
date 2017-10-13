using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class ChargeProjectile:Attack 
	{
		public ShotThresholds shotThresholds;
		public EnergyCosts energyCosts;
		public ProjectileProperties baseProjectile;
		public ProjectileProperties mediumProjectile;
		public ProjectileProperties largeProjectile;
		public SpriteRenderer spriteToFlash;

		//[HideInInspector]
		public float currentChargeTime = 0.0f;

		[System.Serializable]
		public class ShotThresholds
		{
			public float timeBeforeMediumShot = 1.0f;
			public float timeBeforeLargeShot = 2.5f;
		}

		[System.Serializable]
		public class EnergyCosts
		{
			public int regular = 1;
			public int medium = 2;
			public int large = 4;
		}

		void Update()
		{
			//Check to see if we're attempting to Begin this attack
			if(Time.timeScale > 0.0f && slots.actor && isEnabled)
			{
				if(slots.actor.slots.input)
				{
					if(((slots.actor.slots.input.isAttackButtonDown && attackInputImportance == AttackImportance.Primary || attackInputImportance == AttackImportance.Both)) || (slots.actor.slots.input.isSubAttackButtonDown && (attackInputImportance == AttackImportance.Sub || attackInputImportance == AttackImportance.Both)))
					{
						currentChargeTime += Time.deltaTime;
					}
					else
					{
						if(currentChargeTime >= shotThresholds.timeBeforeLargeShot && (slots.actor.mp == null || slots.actor.mp.current >= energyCosts.large))
						{
							projectile = largeProjectile;
							currentChargeTime = 0.0f;
							Begin();

							if(slots.actor.mp)
							{
								slots.actor.mp.Decrement(energyCosts.large);
							}
						}
						else if(currentChargeTime >= shotThresholds.timeBeforeMediumShot && (slots.actor.mp == null || slots.actor.mp.current >= energyCosts.medium))
						{
							projectile = mediumProjectile;
							currentChargeTime = 0.0f;
							Begin();

							if(slots.actor.mp)
							{
								slots.actor.mp.Decrement(energyCosts.medium);
							}
						}
						else if(currentChargeTime > 0.0f && (slots.actor.mp == null || slots.actor.mp.current >= energyCosts.regular))
						{
							projectile = baseProjectile;
							currentChargeTime = 0.0f;
							Begin();

							if(slots.actor.mp)
							{
								slots.actor.mp.Decrement(energyCosts.regular);
							}
						}

						currentChargeTime = 0.0f;
					}

					FlashSprite();
				}
			}
		}

		protected void FlashSprite()
		{
			if(!spriteToFlash)
			{
				return;
			}

			if(currentChargeTime > 0.0f)
			{
				float flashDuration = 0.2f;
				float remainder = currentChargeTime % flashDuration;
				if(remainder > flashDuration / 2.0f)
				{
					spriteToFlash.color = new Color(0.5f, 0.5f, 0.5f);
				}
				else
				{
					spriteToFlash.color = new Color(1.0f, 1.0f, 1.0f);
				}
			}
			else
			{
				spriteToFlash.color = new Color(1.0f, 1.0f, 1.0f);
			}
		}
	}
}
