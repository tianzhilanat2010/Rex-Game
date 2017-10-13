/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RexEngine
{
	//The base RexEngine object class. Extended by RexActor.
	[SelectionBase]
	public class RexObject:MonoBehaviour 
	{
		//Primarily used to determine the side a collision happened on
		public enum Side
		{
			None,
			Left,
			Right,
			Top,
			Bottom
		}

		//Used to check against collision type
		public enum CollisionType
		{
			Enter,
			Stay,
			Exit
		}

		//This is for the most common things that need to be slotted on a RexObject, to keep them all in one easy place
		[System.Serializable]
		public class Slots
		{
			public Animator anim;
			public SpriteRenderer spriteRenderer;
			public Transform spriteHolder;
			public RexController controller;
			public RexInput input;
			public RexPhysics physicsObject;
			public Collider2D collider;
			public Jitter jitter;
		}

		public Slots slots;

		//If the object despawns when it exits a scene
		[HideInInspector]
		public bool willDespawnOnSceneExit = true;

		//Sets the position of the object, including notifying its RexPhysics and its RexController if necessary
		public void SetPosition(Vector2 position)
		{
			transform.position = position;

			if(slots.physicsObject)
			{
				slots.physicsObject.properties.position = position;
				slots.physicsObject.previousFrameProperties.position = position;
			}

			if(slots.controller)
			{
				slots.controller.aerialPeak = transform.position.y;
			}
		}

		//Plays a sound effect only if the object playing the sound effect is currently within the boundaries of the main camera
		public void PlaySoundIfOnCamera(AudioClip clip, float pitch = 1.0f, AudioSource source = null)
		{
			if(CameraHelper.CameraContainsPoint(transform.position, 1.5f))
			{
				AudioSource newSource = (source == null) ? GetComponent<AudioSource>() : source;
				if(newSource != null)
				{
					newSource.pitch = pitch;
					newSource.PlayOneShot(clip);
				}
			}
		}

		//Called when an object is moving out of a scene, but hasn't fully left; can be overidden to have unique effects here
		public virtual void OnSceneBoundaryCollisionInsideBuffer(){}

		//This is called when PhysicsManager.gravityScale is changed
		public virtual void OnGravityScaleChanged(float _gravityScale)
		{
			float scaleMultiplier = (_gravityScale >= 0.0f) ? 1.0f : -1.0f;
			transform.localScale = new Vector3(transform.localScale.x, scaleMultiplier, 1.0f);

			if(slots.controller)
			{
				slots.controller.AnimateGravityFlip();
			}

			BoxCollider2D collider = GetComponent<BoxCollider2D>();
			if(collider)
			{
				if(Mathf.Abs(collider.offset.y) > 0.0f)
				{
					//This handles a specific edgecase where gravity reverses while the actor is crouching down in a one-tile-high passageway; it ensures that they retain the same position and don't leave the crouching state
					SetPosition(new Vector2(transform.position.x, transform.position.y + collider.offset.y * slots.controller.GravityScaleMultiplier()));
				}
			}
		}

		//This can be overidden by individual objects to have them do unique things when their RexPhysics component encounters different types of collisions
		public virtual void OnPhysicsCollision(Collider2D col, Side side, CollisionType type){}

		//This can be overidden by individual objects to have them do unique things when another RexPhysics component collides with it
		public virtual void NotifyOfCollisionWithPhysicsObject(Collider2D col, Side side, CollisionType type){}

		//This can be overidden by individual objects to clean them up in various ways
		public virtual void Clear()
		{
			Destroy(gameObject);
		}
	}

}