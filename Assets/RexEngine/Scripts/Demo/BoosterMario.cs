using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RexEngine;

public class BoosterMario:RexActor
{
	void Start() 
	{
		DontDestroyOnLoad(gameObject);
	}
}
