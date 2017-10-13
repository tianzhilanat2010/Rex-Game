/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class DemoEnemy:Enemy 
	{
		protected float deathVelocity = 15.0f;

		void FixedUpdate()
		{
			if(isDead)
			{
				if(slots.spriteHolder && slots.physicsObject.isEnabled)
				{
					slots.physicsObject.SetVelocityX(deathVelocity);
					slots.spriteHolder.transform.localEulerAngles = new Vector3(0.0f, 0.0f, slots.spriteHolder.transform.localEulerAngles.z + 5.0f);
				}
			}
		}

		protected override void OnDeath()
		{
			if(gameObject.name != "Ankylosaur")
			{
				slots.physicsObject.isEnabled = true;
				slots.physicsObject.RemoveFromCollisions("Terrain");
				if(GameManager.Instance.player.transform.position.x > transform.position.x)
				{
					deathVelocity *= -1.0f;
				}

				slots.physicsObject.gravitySettings.usesGravity = true;
				slots.physicsObject.SetVelocityX(deathVelocity);
				slots.physicsObject.AddVelocityForSingleFrame(new Vector2(0.0f, 30.0f));
			}
		}
	}
}
