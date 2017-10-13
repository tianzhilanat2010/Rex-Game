/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RexEngine
{
	public class Enemy:RexActor
	{
		protected override void OnDeath()
		{
			DropSpawner dropSpawner = GetComponent<DropSpawner>();
			if(dropSpawner != null)
			{
				dropSpawner.DropObject();
			}
		}
	}
}
