/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RexEngine;

public class Demo_9_LevelScript:MonoBehaviour 
{
	public Door door;

	void Start() 
	{
		if(DataManager.Instance.hasVisitedBonusRoom_15)
		{
			Debug.Log("Previous level is bonus room; disabling Door");
			door.GetComponent<BoxCollider2D>().enabled = false;
		}
	}
}
