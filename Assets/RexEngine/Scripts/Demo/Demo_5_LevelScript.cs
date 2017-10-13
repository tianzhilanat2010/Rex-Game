/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RexEngine;

public class Demo_5_LevelScript:MonoBehaviour 
{
	public SceneLoader bonusRoomLoader;

	void Start() 
	{
		if(DataManager.Instance.hasVisitedBonusRoom_6)
		{
			Debug.Log("Previous level is bonus room; disabling scene loader");
			bonusRoomLoader.sceneLoadType = SceneLoader.SceneLoadType.EntranceOnly;
		}
	}
}
