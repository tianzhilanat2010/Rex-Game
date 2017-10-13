/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager:MonoBehaviour 
{
	[System.NonSerialized]
	public string lastCheckpointID;

	[System.NonSerialized]
	public string lastSavedScene = "Demo_1";

	[System.NonSerialized]
	public int deaths = 0;

	[System.NonSerialized]
	public bool hasVisitedBonusRoom_6;

	[System.NonSerialized]
	public bool hasVisitedBonusRoom_12;

	[System.NonSerialized]
	public bool hasVisitedBonusRoom_15;

	[System.NonSerialized]
	public bool hasUnlockedBounce;

	[System.NonSerialized]
	public bool hasUnlockedProjectile;

	[System.NonSerialized]
	public bool hasUnlockedDoubleJump;

	[System.NonSerialized]
	public bool hasUnlockedFly;

	[System.NonSerialized]
	public bool hasUnlockedWallCling;

	private static DataManager instance = null;
	public static DataManager Instance 
	{ 
		get 
		{
			if(instance == null)
			{
				GameObject go = new GameObject();
				instance = go.AddComponent<DataManager>();
				go.name = "DataManager";
			}

			return instance; 
		} 
	}

	void Awake()
	{
		if(instance == null)
		{
			instance = this;
		}

		DontDestroyOnLoad(gameObject);
	}

	public void Save()
	{
		//PlayerPrefs.SetString("LastCheckpointID", lastCheckpointID);
		//PlayerPrefs.SetString("LastSavedScene", lastSavedScene);
		//PlayerPrefs.SetInt("Deaths", deaths);
		PlayerPrefs.SetInt("HasVisitedBonusRoom_6", hasVisitedBonusRoom_6 ? 1 : 0);
		PlayerPrefs.SetInt("HasVisitedBonusRoom_12", hasVisitedBonusRoom_12 ? 1 : 0);
		PlayerPrefs.SetInt("HasVisitedBonusRoom_15", hasVisitedBonusRoom_15 ? 1 : 0);
		PlayerPrefs.SetInt("HasUnlockedBounce", hasUnlockedBounce ? 1 : 0);
		PlayerPrefs.SetInt("HasUnlockedProjectile", hasUnlockedProjectile ? 1 : 0);
		PlayerPrefs.SetInt("HasUnlockedDoubleJump", hasUnlockedDoubleJump ? 1 : 0);
		PlayerPrefs.SetInt("HasUnlockedFly", hasUnlockedFly ? 1 : 0);
		PlayerPrefs.SetInt("HasUnlockedWallCling", hasUnlockedWallCling ? 1 : 0);
	}

	public void Load()
	{
		//lastCheckpointID = PlayerPrefs.GetString("LastCheckpointID");
		//lastSavedScene = PlayerPrefs.GetString("LastSavedScene");
		//deaths = PlayerPrefs.GetInt("Deaths");
		hasVisitedBonusRoom_6 = (PlayerPrefs.GetInt("HasVisitedBonusRoom_6") == 1) ? true : false;
		hasVisitedBonusRoom_12 = (PlayerPrefs.GetInt("HasVisitedBonusRoom_12") == 1) ? true : false;
		hasVisitedBonusRoom_15 = (PlayerPrefs.GetInt("HasVisitedBonusRoom_15") == 1) ? true : false;
		hasUnlockedBounce = (PlayerPrefs.GetInt("HasUnlockedBounce") == 1) ? true : false;
		hasUnlockedProjectile = (PlayerPrefs.GetInt("HasUnlockedProjectile") == 1) ? true : false;
		hasUnlockedDoubleJump = (PlayerPrefs.GetInt("HasUnlockedDoubleJump") == 1) ? true : false;
		hasUnlockedFly = (PlayerPrefs.GetInt("HasUnlockedFly") == 1) ? true : false;
		hasUnlockedWallCling = (PlayerPrefs.GetInt("HasUnlockedWallCling") == 1) ? true : false;
	}

	public void ResetData()
	{
		lastCheckpointID = "A";
		lastSavedScene = "Demo_1";
		deaths = 0;
		hasVisitedBonusRoom_6 = false;
		hasVisitedBonusRoom_12 = false;
		hasVisitedBonusRoom_15 = false;
		hasUnlockedBounce = false;
		hasUnlockedProjectile = false;
		hasUnlockedDoubleJump = false;
		hasUnlockedFly = false;
		hasUnlockedWallCling = false;
		Save();
	}
}
