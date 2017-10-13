using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class BreakBottomBlock:RexActor
	{
		protected bool hasDroppedItem = false;

		public override void NotifyOfCollisionWithPhysicsObject(Collider2D col, Side side, CollisionType type)
		{
			if(col.tag == "Player")
			{
				if((side == Side.Bottom && PhysicsManager.Instance.gravityScale >= 0.0f) || (side == Side.Top && PhysicsManager.Instance.gravityScale <= 0.0f))
				{
					if(!hasDroppedItem)
					{
						hasDroppedItem = true;
						slots.jitter.Play(2, Jitter.JitterStrength.Mild);
						Damage(1);
					}
				}
			}
		}

		protected override void OnDeath()
		{
			DropSpawner dropSpawner = GetComponent<DropSpawner>();
			if(dropSpawner != null)
			{
				GameObject drop = dropSpawner.DropObject();
				RexObject rexObject = drop.GetComponent<RexObject>();
				if(rexObject != null)
				{
					float yPosition = transform.position.y;
					if(slots.collider != null)
					{
						yPosition = slots.collider.bounds.max.y;
						if(rexObject.slots.collider)
						{
							yPosition = slots.collider.bounds.max.y + rexObject.slots.collider.bounds.size.y * 0.5f;
						}
					}

					rexObject.SetPosition(new Vector2(transform.position.x, yPosition));
				}
			}
		}
	}
}
