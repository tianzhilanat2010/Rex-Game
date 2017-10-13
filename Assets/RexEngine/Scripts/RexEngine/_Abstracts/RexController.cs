/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RexEngine
{
	public class RexController:MonoBehaviour
	{
		//This is for the most common things that need to be slotted on a RexObject, to keep them all in one easy place
		[System.Serializable]
		public class Slots
		{
			public RexPhysics physicsObject;
			public RexActor actor;
			public RexInput input;
			public Animator anim;
			public AudioSource audio;
		}

		[System.Serializable]
		public class Animations
		{
			public AnimationClip defaultAnimation;
			public AnimationClip fallingAnimation;
			public AnimationClip deathAnimation;
			public AnimationClip idleAnimation;
			public AnimationClip onEnabledAnimation;
		}

		[System.Serializable]
		public class AudioClips
		{
			public AudioClip deathClip;
		}

		[System.Serializable]
		public class TurnAnimations
		{
			public AnimationClip groundAnimation;
			public AnimationClip airAnimation;
			public AnimationClip crouchAnimation;
			public AnimationClip gravityFlipAnimation;
		}

		public Slots slots;
		public Animations animations; //AnimationClips for the basic animations a Controller uses
		public TurnAnimations turnAnimations; //AnimationClips for the grounded and aerial turn animations
		public AudioClips audioClips;
		public float overrideMaxFallSpeed = 0.0f; //If this is greater than 0, it will override the gravitySettings.maxFallSpeed value on the attached RexPhysics
		public float stunDuration = 1.0f;
		public bool willPausePhysicsOnEnable = false;

		[HideInInspector]
		public float aerialPeak; //The highest point this reaches during a jump or fall

		[HideInInspector]
		public int framesSinceDrop = 10; //The number of frames since the player dropped from a ladder

		[HideInInspector]
		public bool isEnabled = true; //If isEnabled is False, this will not update movement for its RexStates

		[HideInInspector]
		public bool isTurning = false; //Whether the Controller is in the middle of turning left > right, or vice versa

		[HideInInspector]
		public bool isKnockbackActive = false; //Whether the Controller currently has knockback active

		[HideInInspector]
		public bool isStunned; //In a stunned state with minimal input allowed, but not being damaged or knocked back

		[HideInInspector]
		public Vector2 axis; //The directional axis this will move in if no input is slotted; -1.0f is left, 1.0f is right, 0.0f is neutral

		[System.NonSerialized]
		public bool isDashing = false; //Whether a Dash is currently active

		[HideInInspector]
		public RexState currentState; //The current RexState being updated by this Controller

		[HideInInspector]
		public RexState previousState; //The previous RexState

		[HideInInspector]
		public Direction direction = new Direction();

		[HideInInspector]
		public BounceState bounceState;

		protected List<RexState> states; //A list of RexStates attached to this controller which only update if they're the currentState
		protected List<RexState> concurrentStates; //A list of RexStates attached to this controller which can still update in the background, even if they aren't set to the currentState
		protected DefaultState defaultState;
		protected FallingState fallingState;
		protected DeathState deathState;
		protected MovingState movingState;
		protected LandingState landingState;

		void Awake()
		{
			aerialPeak = transform.position.y;

			if(slots.actor == null)
			{
				slots.actor = GetComponent<RexActor>();
			}

			if(slots.physicsObject == null)
			{
				slots.physicsObject = GetComponent<RexPhysics>();
			}

			//Buffering any MovingStates or LandingStates attached to this so we don't have to call GetComponent constantly
			movingState = GetComponent<MovingState>();
			landingState = GetComponent<LandingState>();

			if(states == null)
			{
				states = new List<RexState>();
			}

			if(concurrentStates == null)
			{
				concurrentStates = new List<RexState>();
			}

			//Add all states with isConcurrent marked to the concurrentStates list, and all other states to states list, for easier and faster updates
			RexState[] stateComponents = GetComponents<RexState>();

			//Create default, falling, and death states automatically, since every RexController needs them
			defaultState = gameObject.AddComponent(typeof(DefaultState)) as DefaultState;
			defaultState.animation = animations.defaultAnimation;

			fallingState = gameObject.AddComponent(typeof(FallingState)) as FallingState;
			fallingState.animation = animations.fallingAnimation;

			deathState = gameObject.AddComponent(typeof(DeathState)) as DeathState;
			deathState.animation = animations.deathAnimation;
			deathState.audioClip = audioClips.deathClip;

			//Automatically make this face left or right based on whether or not the transform is scaled to a negative or not
			direction.horizontal = (slots.actor && slots.actor.transform.localScale.x >= 0.0f) ? Direction.Horizontal.Right : Direction.Horizontal.Left;


		}

		void Start()
		{
			//Initialize, with DefaultState being the intro state
			SetState(defaultState);
		}

		void Update()
		{
			if(slots.input)
			{
				axis = new Vector2(slots.input.horizontalAxis, slots.input.verticalAxis);
				CheckForOneWayPlatforms();
			}
		}

		void FixedUpdate() 
		{
			if(Time.timeScale > 0 && isEnabled && slots.actor && !slots.actor.isDead)
			{
				ApplyPhysicsObjectFlags();
				if(currentState.isEnabled) //Update the state set as currentState
				{
					currentState.UpdateMovement();
				}

				for(int i = 0; i < concurrentStates.Count; i ++)
				{
					if(concurrentStates[i].isEnabled && concurrentStates[i] != currentState) //For any states marked as concurrent, update them in the background
					{
						concurrentStates[i].UpdateMovement();
					}
				}
			}
		}

		#region public methods
		//Attempts to set currentState to a new state
		public void SetState(RexState  _state, bool canInterruptSelf = false)
		{ 
			bool isStateBlocked = false;
			if(_state.id == KnockbackState.idString && !currentState.isKnockbackEnabled)
			{
				isStateBlocked = true;
			}

			if(currentState && currentState.id == CrouchState.idString && !currentState.GetComponent<CrouchState>().CanExitCrouch())
			{
				isStateBlocked = true;
			}

			if(_state.id == KnockbackState.idString && isStateBlocked)
			{
				if(slots.audio && _state.audioClip) //Play the damaged sound even if knockback itself doesn't happen
				{
					slots.actor.PlaySoundIfOnCamera(_state.audioClip, 1.0f, slots.audio);
				}
			}

			if(isStateBlocked || slots.actor && slots.actor.isDead || !_state.isEnabled)
			{
				return;
			}

			if(_state != currentState || canInterruptSelf)
			{
				_state.hasEnded = false;

				if(_state != null)
				{
					previousState = currentState;
					currentState = _state;
				}

				if(previousState)
				{
					if(_state.id == KnockbackState.idString)
					{
						isKnockbackActive = true;
						previousState.End();
					}

					previousState.OnStateChanged();
				}

				if(!_state.IsTurnAnimationOverriding())
				{
					CancelTurn();
				}

				if(slots.actor)
				{
					slots.actor.OnStateChanged(_state);
					slots.actor.OnStateEntered(_state);
					slots.actor.OnStateExited(previousState);
				}

				_state.OnBegin();

				if(slots.audio && _state.audioClip)
				{
					slots.actor.PlaySoundIfOnCamera(_state.audioClip, 1.0f, slots.audio);
				}

				/*if(slots.actor.tag == "Player" && previousState)
					Debug.Log(slots.actor.gameObject.name + " :: Begin State " + _state.id + "   from: " + previousState.id);*/
				
				if((_state.willPlayAnimationOnBegin || slots.actor.currentAttack != null) && (!isKnockbackActive || _state.id == KnockbackState.idString))
				{
					//Debug.Log("Playing animation for state: " + _state.id);
					_state.PlayAnimation();
				}
			}
		}

		public void SetAxis(Vector2 _axis) //This is most commonly set by a RexInput component, which sends this the results from a keyboard or d-pad
		{
			axis = _axis;
		}

		public void Stun() //While stunned, a Controller can't move
		{
			StartCoroutine("StunCoroutine");
			slots.physicsObject.SetVelocityX(0);
			slots.physicsObject.SetVelocityY(0);
		}

		protected IEnumerator StunCoroutine()
		{
			isStunned = true;
			yield return new WaitForSeconds(stunDuration);

			isStunned = false;
		}

		public string StateID() //Returns the ID of currentState
		{
			return (currentState) ? currentState.id : "";
		}

		public void AddState(RexState _state)
		{
			if(concurrentStates == null)
			{
				concurrentStates = new List<RexState>();
			}

			if(states == null)
			{
				states = new List<RexState>();
			}

			bool isStateAlreadyAdded = false;
			if(_state.isConcurrent)
			{
				for(int i = 0; i < concurrentStates.Count; i ++)
				{
					if(concurrentStates[i].id == _state.id)
					{
						isStateAlreadyAdded = true;
						return;
					}
				}

				if(!isStateAlreadyAdded)
				{
					concurrentStates.Add(_state);
				}
			}
			else
			{
				for(int i = 0; i < states.Count; i ++)
				{
					if(states[i].id == _state.id)
					{
						isStateAlreadyAdded = true;
						return;
					}
				}

				if(!isStateAlreadyAdded)
				{
					states.Add(_state);
				}
			}

			if(isStateAlreadyAdded)
			{
				return;
			}

			for(int i = 0; i < concurrentStates.Count; i ++)
			{
				concurrentStates[i].OnNewStateAdded(_state);
			}

			for(int i = 0; i < states.Count; i ++)
			{
				states[i].OnNewStateAdded(_state);
			}

			if(_state.id == BounceState.idString)
			{
				bounceState = _state.GetComponent<BounceState>();
			}
		}

		public void SetToAlive() //Used to set this to alive again after the attached RexActor has been killed
		{
			SetStateToDefault();
			slots.physicsObject.isEnabled = true;
		}

		public void SetToDead() //Used to set this to dead; most commonly called by RexActor when its HP drops to 0 or it otherwise is killed
		{
			EndAllStates();

			DeathState deathState = GetComponent<DeathState>();
			if(deathState)
			{
				deathState.Begin();
			}
		}

		public void EndAllStates() //Ends movements for every single attached RexState at once
		{
			CancelTurn();
			StopAllCoroutines();

			for(int i = 0; i < states.Count; i ++)
			{
				states[i].End();
			}

			for(int i = 0; i < concurrentStates.Count; i ++)
			{
				concurrentStates[i].End();
			}
		}

		public void SetStateToDefault(bool canInterruptSelf = false) //Sets currentState either to DefaultState or to FallingState depending on whether or not it is grounded
		{
			RexState movement = defaultState;
			bool canMoveVertically = (movingState && movingState.canMoveVertically);
			if(slots.physicsObject && (!slots.physicsObject.IsOnSurface() && (slots.physicsObject.gravitySettings.usesGravity && !canMoveVertically)))
			{
				movement = fallingState;
			}

			if(!isKnockbackActive)
			{
				SetState(movement, canInterruptSelf);
			}
		}

		public void CancelTurn() //Stops a Turn from happening
		{
			isTurning = false;
			StopCoroutine("TurnCoroutine");
		}

		public void FaceDirection(Direction.Horizontal _direction) //Called when the Controller is moved left or right, and thus might change direction; calls Turn() if we did change
		{
			//We changed directions
			if(_direction != direction.horizontal)
			{
				if(CanChangeDirection())
				{
					Turn();
				}
			}
		}

		public void OnAttackComplete()
		{
			if(currentState)
			{
				currentState.OnAttackComplete();
			}
		}

		public virtual void Turn() //The visual component of a Turn; scales the X axis to positive or negative, and plays our slotted Turn AnimationClip
		{
			Direction.Horizontal _direction = (direction.horizontal == Direction.Horizontal.Left) ? Direction.Horizontal.Right : Direction.Horizontal.Left;
			float scaleMultiplier = (_direction == Direction.Horizontal.Left) ? -1.0f : 1.0f;
			slots.actor.transform.localScale = new Vector3(scaleMultiplier, slots.actor.transform.localScale.y, slots.actor.transform.localScale.z);
			direction.horizontal = _direction;

			if(slots.actor.currentAttack != null && slots.actor.currentAttack.canceledBy.onTurn)
			{
				slots.actor.currentAttack.Cancel();
			}

			StopCoroutine("TurnCoroutine");
			StartCoroutine("TurnCoroutine");
		}

		public virtual void AnimateGravityFlip()
		{
			StopCoroutine("GravityFlipCoroutine");
			StartCoroutine("GravityFlipCoroutine");
		}

		public virtual void AnimateEnable()
		{
			if(enabled)
			{
				StopCoroutine("AnimateEnableCoroutine");
				StartCoroutine("AnimateEnableCoroutine");
			}
		}

		public virtual bool CanChangeDirection() //Checks to see if it's possible for us to change horizontal directions
		{
			if(isKnockbackActive || (slots.actor.currentAttack != null && !slots.actor.currentAttack.actionsAllowedDuringAttack.turning))
			{
				return false;
			}
			else if(currentState != null && !currentState.willAllowDirectionChange)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public bool IsOveriddenByCrouch()
		{
			bool isOveriddenByCrouch = false;
			if(StateID() == CrouchState.idString)
			{
				CrouchState crouchState = GetComponent<CrouchState>();
				if(crouchState && !crouchState.WillAllowMovement())
				{
					isOveriddenByCrouch = true;
				}
			}

			return isOveriddenByCrouch;
		}

		public void Knockback(Direction.Horizontal _direction) //Called directly to apply knockback if a KnockbackState component is attached
		{
			KnockbackState knockback = GetComponent<KnockbackState>();
			if(knockback)
			{
				knockback.knockbackDirection = _direction;
				knockback.Begin();
			}
		}

		public float GravityScaleMultiplier() //Returns 1.0 if gravity is normal, and -1.0 if gravity is reversed
		{
			return (slots.physicsObject && slots.physicsObject.GravityScale() > 0.0f) ? 1.0f : -1.0f;
		}
		#endregion

		#region private methods

		protected void ApplyPhysicsObjectFlags() //Checks for Landing from a jump or fall, as well as a few other common physics things
		{
			if(!slots.physicsObject)
			{
				return;
			}

			if(slots.physicsObject.DidLandThisFrame() || (StateID() == JumpState.idString && slots.physicsObject.IsOnSurface() && currentState.GetComponent<JumpState>().CanEnd()))
			{
				float distanceFallen = Mathf.Abs(aerialPeak - transform.position.y);
				float minDistanceToTriggerLanding = 0.125f;
				if(distanceFallen > minDistanceToTriggerLanding)
				{
					bool canMoveVertically = (movingState && movingState.canMoveVertically);
					if(landingState != null && !isKnockbackActive && !canMoveVertically && StateID() != DefaultState.idString)
					{
						landingState.CheckStun(distanceFallen);
						landingState.Begin();
					}
				}

				if(!isKnockbackActive && StateID() != LandingState.idString && StateID() != LadderState.idString)
				{
					SetStateToDefault();
				}
			}

			if(slots.physicsObject.IsOnSurface())
			{
				aerialPeak = transform.position.y;
				if(StateID() == FallingState.idString)
				{
					SetStateToDefault();
				}
			}
			else
			{
				if((slots.physicsObject.GravityScale() > 0.0f && transform.position.y > aerialPeak) || (slots.physicsObject.GravityScale() < 0.0f && transform.position.y < aerialPeak))
				{
					aerialPeak = transform.position.y;
				}
			}

			if(slots.physicsObject.properties.isFalling && !isKnockbackActive && StateID() != LadderState.idString && StateID() != DashState.idString && StateID() != WallClingState.idString) 
			{
				if(!(StateID() == MovingState.idString && movingState.canMoveVertically))
				{
					SetStateToDefault();
				}
			}

			framesSinceDrop ++;
			if(framesSinceDrop > 10)
			{
				framesSinceDrop = 10;
			}
		}

		protected IEnumerator TurnCoroutine() //Turn() should be called rather than calling this directly
		{
			isTurning = true;
			float duration = 0.0f;

			AnimationClip animation = (slots.physicsObject && slots.physicsObject.IsOnSurface()) ? turnAnimations.groundAnimation : turnAnimations.airAnimation;
			if(StateID() == CrouchState.idString)
			{
				animation = turnAnimations.crouchAnimation;
			}

			if(animation != null && slots.physicsObject && StateID() != LadderState.idString)
			{
				duration = animation.length;
				slots.anim.Play(animation.name, 0, 0);
			}

			yield return new WaitForSeconds(duration);

			isTurning = false;
			if(currentState)
			{
				currentState.PlayAnimationForSubstate();
			}
		}

		protected IEnumerator GravityFlipCoroutine()
		{
			isTurning = true;
			float duration = 0.0f;

			if(turnAnimations.gravityFlipAnimation != null && slots.physicsObject && StateID() != LadderState.idString && slots.actor.currentAttack == null)
			{
				duration = turnAnimations.gravityFlipAnimation.length;
				slots.anim.Play(turnAnimations.gravityFlipAnimation.name, 0, 0);
			}

			yield return new WaitForSeconds(duration);

			isTurning = false;
			if(currentState)
			{
				currentState.PlayAnimation();
			}
		}

		protected IEnumerator AnimateEnableCoroutine()
		{
			isTurning = true;

			if(willPausePhysicsOnEnable)
			{
				slots.physicsObject.isEnabled = false;
			}

			float duration = 0.0f;

			if(animations.onEnabledAnimation != null && slots.physicsObject)
			{
				duration = animations.onEnabledAnimation.length;
				slots.anim.Play(animations.onEnabledAnimation.name, 0, 0);
			}

			yield return new WaitForSeconds(duration);

			isTurning = false;

			if(willPausePhysicsOnEnable)
			{
				slots.physicsObject.isEnabled = true;
			}

			if(currentState)
			{
				currentState.PlayAnimation();
			}
		}

		protected void CheckForOneWayPlatforms() //If we're on a one-way platform and we're pressing the proper input to drop through it, drop through it
		{
			if(slots.physicsObject && slots.physicsObject.IsOnSurface() && slots.input && slots.input.isJumpButtonDownThisFrame && slots.input.verticalAxis == -1.0f)
			{
				DropThroughOneWayPlatform();
			}
		}

		protected void DropThroughOneWayPlatform() //Drop through a one-way platform
		{
			if(RaycastHelper.DropThroughFloorRaycast((Direction.Vertical)(GravityScaleMultiplier() * -1.0f), slots.actor.GetComponent<BoxCollider2D>(), GravityScaleMultiplier()))
			{
				StartCoroutine("DisableOneWayPlatformsCoroutine");
			}
		}

		protected IEnumerator DisableOneWayPlatformsCoroutine() //Don't call this directly; this just helps DropThroughOneWayPlatform do its thing
		{
			if(slots.physicsObject)
			{
				slots.physicsObject.DisableOneWayPlatforms();
				yield return new WaitForSeconds(0.25f);

				slots.physicsObject.EnableOneWayPlatforms();
			}
		}

		#endregion
	}

	#region state declarations
	//Below are declarations for the basic states that are auto-generated by RexController on Awake: Default, Falling, and Death
	public class DefaultState:RexState 
	{
		public const string idString = "Default";

		void Awake() 
		{
			id = idString;
			doesTurnAnimationHavePriority = true;
			isKnockbackEnabled = true;
			GetController();
		}

		public override void OnBegin()
		{
			StartCoroutine("IdleCoroutine");
		}

		public override void OnStateChanged()
		{
			StopCoroutine("IdleCoroutine");
		}

		public override void OnEnded()
		{
			StopCoroutine("IdleCoroutine");
		}

		protected IEnumerator IdleCoroutine() //This plays an idle animation if we've been still for 5 seconds or more
		{
			float durationBeforeIdle = 7.5f;
			yield return new WaitForSeconds(durationBeforeIdle);

			if(controller.isEnabled && controller.animations.idleAnimation && controller.StateID() == DefaultState.idString && controller.slots.actor && !controller.slots.actor.IsAttacking())
			{
				PlaySecondaryAnimation(controller.animations.idleAnimation);
				yield return new WaitForSeconds(controller.animations.idleAnimation.length);
			}

			OnIdleAnimationComplete();
		}

		protected void OnIdleAnimationComplete()
		{
			if(controller.animations.idleAnimation && controller.StateID() == "Default")
			{
				PlayAnimation();
				StartCoroutine("IdleCoroutine");
			}
		}
	}


	public class FallingState:RexState 
	{
		public const string idString = "Falling";

		void Awake() 
		{
			id = idString;
			isKnockbackEnabled = true;
			doesTurnAnimationHavePriority = true;
			GetController();
		}
	}

	public class DeathState:RexState 
	{
		public const string idString = "Death";

		void Awake() 
		{
			id = idString;
			GetController();
		}

		public override void OnBegin()
		{
			if(controller.slots.physicsObject) 
			{
				controller.slots.physicsObject.isEnabled = false;
			}
		}
	}

	#endregion
}