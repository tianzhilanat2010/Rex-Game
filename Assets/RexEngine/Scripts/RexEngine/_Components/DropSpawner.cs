using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropSpawner:MonoBehaviour 
{
	public GameObject objectToSpawn;

	public GameObject DropObject()
	{
		GameObject dropObject = Instantiate(objectToSpawn, transform.position, Quaternion.identity).gameObject;
		dropObject.transform.parent = transform.parent;

		return dropObject;
	}
}
