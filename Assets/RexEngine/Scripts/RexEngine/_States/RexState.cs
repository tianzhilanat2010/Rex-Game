/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	//The abstract of States used by RexController to update movement; concrete implementations include Moving, Jumping, and so on
	public class RexState:MonoBehaviour
	{
		[HideInInspector]
		public string id = ""; //The id of a state; should be set in Awake, and should be unique for each state

		[HideInInspector]
		public bool hasEnded; //Whether or not the state has ended its current movements

		public bool isEnabled = true; //If False, this state will not be updated

		public AnimationClip animation; //The AnimationClip that plays in this state
		public AudioClip audioClip; //The AudioClip that plays when this state begins

		[HideInInspector]
		public bool willPlayAnimationOnBegin = true; //If False, you must manually play the animation for this state, rather than it auto-starting when the state begins

		public bool isKnockbackEnabled = true; //If knockback is allowed in this state 

		[HideInInspector]
		public bool willAllowDirectionChange = true; //If changing directions is allowed in this state

		[HideInInspector]
		public bool isConcurrent = false; //If True, this state will be updated in the background even if it isn't the currentState of the Controller; if False, it will not be updated unless it IS currentState of the Controller

		[HideInInspector]
		public RexController controller; //A reference to the RexController that handles and updates this state

		protected bool doesTurnAnimationHavePriority; //If this is True, a presently-occuring turn animation will take priority over playing this state animation

		void Start()
		{
			EnemyAI enemyAI = GetComponent<EnemyAI>();
			if(enemyAI)
			{
				enemyAI.OnNewStateAdded(this);
			}
		}

		#region public methods

		public void ForceBegin() //Force the State to begin, whether or not CanInitiate() returns True
		{ 
			controller.SetState(this, true);
		}

		public void Begin(bool canInterruptSelf = false) //Begins the State, but only if CanInitiate() returns True; canInterruptSelf governs whether this state can Begin() again even while it's already the currentState
		{ 
			if(CanInitiate())
			{
				controller.SetState(this, canInterruptSelf);
			}
		}

		public void End() //Ends the current state and its movements
		{
			StopAllCoroutines();
			hasEnded = true;
			OnEnded();
		}

		public virtual void UpdateMovement() //Overidden by each state; called every FixedUpdate by RexController. Handles the movement associated with the state
		{

		}

		public bool IsTurnAnimationOverriding() //Checks to see if a Turn animation is playing, and if that should override this state's animation
		{
			return (controller.slots.actor && controller.isTurning && doesTurnAnimationHavePriority);
		}

		public virtual bool CanInitiate() //Can be overidden by each state to determine what circumstances the state can be initiated from
		{
			return true;
		}

		public bool IsFrozen() //Returns True if the game is paused or contact delay is happening; used primarily to see if we should accept inputs
		{
			return !(Time.timeScale > 0 && controller.slots.actor != null);
		}

		public virtual void OnNewStateAdded(RexState _state)
		{
			
		}

		//Called automatically when Begin() is successfully called. This can be overidden for each individual movement
		public virtual void OnBegin()
		{

		}

		//This will play the primary animation
		public void PlayAnimation()
		{
			if(controller.slots.anim)
			{
				AnimationClip animationToPlay = animation;
				if(controller.slots.actor.currentAttack != null)
				{
					animationToPlay = controller.slots.actor.currentAttack.GetActorAnimationClip(); //If we're attacking, attempt to use the animation that corresponds to the attack
				}

				if(animationToPlay != null && !IsTurnAnimationOverriding())
				{
					controller.slots.anim.Play(animationToPlay.name, 0, 0);
					//Debug.Log("Playing: " + id);
				}
			}
		}

		//This is called when the state ends; note that states can continue updating even if another state takes priority, so OnEnded won't necessarily be called just because the state changes
		public virtual void OnEnded(){}

		//This is called when the state changes, whether or not the previous state has ended
		public virtual void OnStateChanged(){}

		public virtual void OnAttackComplete()
		{
			PlayAnimationForSubstate();
		}

		public virtual void PlayAnimationForSubstate()
		{
			PlayAnimation();
		}

		#endregion

		#region private methods

		protected void GetController()
		{
			if(!controller)
			{
				controller = GetComponent<RexController>();
			}

			if(controller)
			{
				controller.AddState(this);
			}
		}

		//Returns True if we're in the middle of an attack, and if that attack is preventing us from doing something in this particular State
		protected bool IsLockedForAttack(Attack.ActionType _actionType)
		{
			bool isLockedForAttack = false;
			if(controller.slots.actor.currentAttack != null && !controller.slots.actor.currentAttack.CanInterrupt(_actionType))
			{
				isLockedForAttack = true;
			}

			return isLockedForAttack;
		}

		//Call this to play a specific animation; this is usually used for secondary, temporary animation clips separate from the default slotted animation clip
		protected void PlaySecondaryAnimation(AnimationClip _animation = null)
		{
			if(_animation != null && controller.slots.anim != null && !controller.isKnockbackActive && !IsTurnAnimationOverriding())
			{
				//Debug.Log("Secondary: " + _animation.name);
				controller.slots.anim.Play(_animation.name, 0, 0);
			}
		}

		#endregion
	}

}
