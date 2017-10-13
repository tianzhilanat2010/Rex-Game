/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;

public class CameraHelper:MonoBehaviour 
{
	/*private static CameraHelper instance = null;
	public static CameraHelper Instance 
	{ 
		get 
		{
			if(instance == null)
			{
				//Debug.Log("CameraHelper :: Instantiate");
				GameObject go = new GameObject();
				instance = go.AddComponent<CameraHelper>();
				go.name = "CameraHelper";
			}
			
			return instance; 
		} 
	}*/

     void Awake() 
	{
		//AttachToSingletonsObject();
	}

	public static bool CameraContainsPoint(Vector3 point, float buffer = 0.0f)
	{
		Rect rect = new Rect();
		rect.xMin = GetLeftEdgeOfCamera() - buffer;
		rect.xMax = GetRightEdgeOfCamera() + buffer;
		rect.yMin = GetBottomEdgeOfCamera() - buffer;
		rect.yMax = GetTopEdgeOfCamera() + buffer;
	
		if(rect.Contains(point))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public static Vector3 GetScreenSizeInUnits(Camera _camera = null)
	{
		if(_camera == null)
		{
			_camera = Camera.main;
		}

		Vector3 screenSize = new Vector3(Screen.width, Screen.height, 0.0f);
		Vector3 screenSizeInUnits = new Vector3(_camera.ScreenToWorldPoint(screenSize).x, _camera.ScreenToWorldPoint(screenSize).y, _camera.ScreenToWorldPoint(screenSize).z);
		screenSizeInUnits -= _camera.transform.position;
		screenSizeInUnits *= 2.0f;

		return screenSizeInUnits;
	}

	public static float GetLeftEdgeOfCamera()
	{
		return Camera.main.transform.position.x - GetScreenSizeInUnits().x * 0.5f;
	}
	
	public static float GetRightEdgeOfCamera()
	{
		return Camera.main.transform.position.x + GetScreenSizeInUnits().x * 0.5f;
	}

	public static float GetTopEdgeOfCamera()
	{
		return Camera.main.transform.position.y + GetScreenSizeInUnits().y * 0.5f;
	}

	public static float GetBottomEdgeOfCamera()
	{
		return Camera.main.transform.position.y - GetScreenSizeInUnits().y * 0.5f;
	}

	/*private void AttachToSingletonsObject()
	{
		GameObject singletons;
		if(GameObject.Find("Singletons") == null)
		{
			GameObject go = new GameObject();
			go.name = "Singletons";
			DontDestroyOnLoad(go);
			singletons = go;
		}
		else
		{
			singletons = GameObject.Find("Singletons").gameObject;
		}
		
		gameObject.transform.parent = singletons.transform;
	}*/
}
