﻿/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RexEngine
{
	public class ScreenShake:MonoBehaviour 
	{
		private RexCamera camera;
		private bool isShakeForever;
		private float magnitude;
		private int remainingShakes;
		private float degradationPerShake;
		private float durationPerShake = 0.2f;

		public enum Duration
		{
			Single,
			Timed,
			Forever
		}

		public enum Magnitude
		{
			Small = 50,
			Medium = 100,
			Fierce = 150
		}

		public enum EndingMagnitude
		{
			None = 0,
			Fourth = 25,
			Half = 50,
			Full = 100,
			OneAndAHalf = 150,
			Double = 200
		}

		//Documentation: 
		//A GameplayCamera must call SetCamera() on this for this class to work
		//Shakes will interrupt each other if new shakes are called
		private static ScreenShake instance = null;
		public static ScreenShake Instance 
		{ 
			get 
			{
				if(instance == null)
				{
					GameObject go = new GameObject();
					instance = go.AddComponent<ScreenShake>();
					go.name = "ScreenShake";
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
		}

		public void ShakeIfOnCamera(Transform _transform)
		{
			if(CameraHelper.CameraContainsPoint(_transform.position))
			{
				Shake();
			}
		}

		public void Shake(Magnitude _magnitude = Magnitude.Medium, EndingMagnitude endingMagnitude = EndingMagnitude.Fourth)
		{
			Stop();
			isShakeForever = false;
			ScheduleShake(_magnitude, endingMagnitude, 1);
		}

		public void ShakeForDuration(Magnitude _magnitude = Magnitude.Small, float duration = 5.0f, EndingMagnitude endingMagnitude = EndingMagnitude.Fourth)
		{
			isShakeForever = false;
			int numberOfShakes = (int)(duration / durationPerShake);
			ScheduleShake(_magnitude, endingMagnitude, numberOfShakes);
		}

		public void ShakeForever(Magnitude _magnitude = Magnitude.Small)
		{
			isShakeForever = true;
			ScheduleShake(_magnitude, EndingMagnitude.Full, 1);
		}

		public void Stop()
		{
			StopCoroutine("SingleShakeCoroutine");
			remainingShakes = 0;
			if(camera != null)
			{
				camera.shakeOffset = new Vector2(0, 0);
			}
		}

		public void SetCamera(RexCamera _camera)
		{
			camera = _camera;
		}

		private void ScheduleShake(Magnitude _magnitude = Magnitude.Medium, EndingMagnitude endingMagnitude = EndingMagnitude.Fourth, int numberOfShakes = 1)
		{
			if(camera != null)
			{
				remainingShakes = numberOfShakes;
				magnitude = (float)_magnitude * 0.005f;

				int numberOfDegradationsPerSingleShake = 2;
				float endingMagnitudeFloat = (float)((float)endingMagnitude * 0.005f) * magnitude;
				float totalMagnitudeToDegrade = magnitude - endingMagnitudeFloat;
				degradationPerShake = totalMagnitudeToDegrade / ((numberOfDegradationsPerSingleShake * remainingShakes) - 1);

				PerformSingleShake();
			}
		}

		private void PerformSingleShake()
		{
			if(camera != null)
			{
				camera.shakeOffset = new Vector2(0.0f, 0.0f);
				StartCoroutine("SingleShakeCoroutine");
			}
		}

		private IEnumerator SingleShakeCoroutine()
		{
			float durationPerStep = 0.01f;
			float adjustmentPerFrame = 0.75f;

			while(camera.shakeOffset.y <= magnitude)
			{
				camera.shakeOffset = new Vector2(camera.shakeOffset.x, camera.shakeOffset.y + adjustmentPerFrame);
				yield return new WaitForSeconds(durationPerStep);
			}

			while(camera.shakeOffset.y >= -magnitude)
			{
				camera.shakeOffset = new Vector2(camera.shakeOffset.x, camera.shakeOffset.y - adjustmentPerFrame);
				yield return new WaitForSeconds(durationPerStep);
			}

			while(camera.shakeOffset.y <= 0.0f)
			{
				camera.shakeOffset = new Vector2(camera.shakeOffset.x, camera.shakeOffset.y + adjustmentPerFrame);
				yield return new WaitForSeconds(durationPerStep);
			}

			camera.shakeOffset = new Vector2(0.0f, 0.0f);

			while(camera.shakeOffset.x <= magnitude)
			{
				camera.shakeOffset = new Vector2(camera.shakeOffset.x + adjustmentPerFrame, camera.shakeOffset.y);
				yield return new WaitForSeconds(durationPerStep);
			}

			while(camera.shakeOffset.x >= -magnitude)
			{
				camera.shakeOffset = new Vector2(camera.shakeOffset.x - adjustmentPerFrame, camera.shakeOffset.y);
				yield return new WaitForSeconds(durationPerStep);
			}

			while(camera.shakeOffset.x <= 0.0f)
			{
				camera.shakeOffset = new Vector2(camera.shakeOffset.x + adjustmentPerFrame, camera.shakeOffset.y);
				yield return new WaitForSeconds(durationPerStep);
			}

			camera.shakeOffset = new Vector2(0.0f, 0.0f);
			OnShakeComplete();
		}

		private void OnShakeComplete()
		{
			if(!isShakeForever)
			{
				remainingShakes --;
			}

			if(remainingShakes > 0)
			{
				DegradeShake();
				PerformSingleShake();
			}
		}

		private void DegradeShake()
		{
			magnitude -= degradationPerShake;
		}
	}

}
