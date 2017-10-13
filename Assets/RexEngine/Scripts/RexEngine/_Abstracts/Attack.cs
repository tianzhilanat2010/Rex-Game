/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class Attack:MonoBehaviour 
	{
		[System.Serializable]
		public class AttackAnimations
		{
			public AnimationClip standing;
			public AnimationClip moving;
			public AnimationClip dashing;
			public AnimationClip jumping;
			public AnimationClip climbing;
			public AnimationClip crouching;
			public AnimationClip wallClinging;
		}

		//If any of these are True, transitioning into that event will cancel this attack
		[System.Serializable]
		public class CanceledBy
		{
			public bool onMove;
			public bool onJump;
			public bool onTurn;
			public bool onKnockback;
			public bool onDash;
			public bool onCrouch;
			public bool onWallCling;
		}

		//What additional actions are canceled if you begin this attack during them?
		[System.Serializable]
		public class Cancels
		{
			public bool dash;
			public bool crouch;
			public bool wallClinging;
		}

		[System.Serializable]
		public class ProjectileProperties
		{
			public RexPool rexPool; //The RexPool that spawns the projectile
			public int limitInstances; //The max number of this projectile you can have active at once
			public bool willAutoCreateProjectile = true; //If a projectile is slotted, setting this to "true" means it will be auto-spawned as soon as the attack begins. Otherwise, it will need to be spawned manually.
			public bool isAimable = false; //Whether or not 8-way aiming with the d-pad is enabled
		}

		//Whether the attack is triggered with the primary or secondary attack button
		public enum AttackImportance
		{
			Primary,
			Sub,
			Both
		}

		public enum AttackDirection
		{
			Ahead,
			Behind
		}

		//If any of these are True, you can do them while the attack is active
		[System.Serializable]
		public class ActionsAllowedDuringAttack
		{
			public bool groundMoving;
			public bool airMoving;
			public bool jumping;
			public bool turning;
			public bool attacking;
			public bool dashing;
			public bool climbing;
			public bool crouching;
			public bool wallClinging;
		}

		//What other states can you use this attack from? 
		[System.Serializable]
		public class CanInitiateFrom
		{
			public bool standing;
			public bool moving;
			public bool jumping;
			public bool dashing;
			public bool climbing;
			public bool crouching;
			public bool wallClinging;
		}

		[System.Serializable]
		public class Slots
		{
			public RexActor actor;
			public Animator animator;
			public AudioSource audio;
			public SpriteRenderer spriteRenderer;
			public BoxCollider2D boxCollider;
		}

		public bool isEnabled; //Whether or not the attack can be used
		public Slots slots;
		public ProjectileProperties projectile;
		public AttackAnimations actorAnimations;
		public AttackAnimations attackAnimations;
		public AudioClip audioClip;
		public ActionsAllowedDuringAttack actionsAllowedDuringAttack;
		public CanceledBy canceledBy;
		public Cancels cancels;
		public CanInitiateFrom canInitiateFrom;
		public AttackImportance attackInputImportance; //if an Input is attached, this governs whether the attach is called on the Attack or the SubAttack button
		public int cooldownFrames; //Frames between the ability to repeat this attack. If this is 0, you can repeat it immediately.
		public bool willAutoEnableCollider = true; //If False, you need to manually enable the collider for this attack when it is executed
		public bool willSyncMoveAnimation = true;
		public float crouchOffset = 0.0f;

		[HideInInspector]
		public AttackDirection attackDirection;

		[HideInInspector]
		public int currentCooldownFrame;

		protected Vector3 nonCrouchingPosition;

		public enum ActionType
		{
			GroundMoving,
			AirMoving,
			Jumping,
			Turning,
			Dashing,
			Climbing,
			Crouching,
			WallClinging
		}

		[HideInInspector]
		public bool isAttackActive;

		void Awake()
		{
			Reset();
			nonCrouchingPosition = transform.localPosition;
		}

		void Update()
		{
			//Check to see if we're attempting to Begin this attack
			if(Time.timeScale > 0.0f && slots.actor && isEnabled)
			{
				if(slots.actor.slots.input)
				{
					if((slots.actor.slots.input.isAttackButtonDownThisFrame && (attackInputImportance == AttackImportance.Primary || attackInputImportance == AttackImportance.Both)) || (slots.actor.slots.input.isSubAttackButtonDownThisFrame && (attackInputImportance == AttackImportance.Sub || attackInputImportance == AttackImportance.Both)))
					{
						Begin();
					}
				}
			}
		}

		void FixedUpdate()
		{
			//Govern attack cooldowns
			if(Time.timeScale > 0.0f && slots.actor)
			{
				currentCooldownFrame --;
				if(currentCooldownFrame <= 0)
				{
					currentCooldownFrame = 0;
				}

				if(isAttackActive)
				{
					SetCrouchPosition();
				}
			}
		}

		#region public methods

		//Forces the attack to begin, even if we'd normally be prevented from initiating it. Use with caution.
		public void ForceBegin()
		{
			Reset();

			if(slots.actor.slots.controller.StateID() == WallClingState.idString)
			{
				if(slots.actor.slots.controller.direction.horizontal == Direction.Horizontal.Right)
				{
					transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
				}
				else
				{
					transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
				}
			}

			SetCrouchPosition();

			StartCoroutine("AttackCoroutine");
		}

		//Called to start the attack after checking if we can initiate it first
		public void Begin()
		{
			if(CanInitiate())
			{
				ForceBegin();
			}
		}

		//Stops the attack
		public void Cancel()
		{
			Reset();
		}

		//Checks to see if we can Begin the attack or if something is stopping us
		public bool CanInitiate()
		{
			string stateID = slots.actor.slots.controller.StateID();
			bool canInitiate = true;
			if(currentCooldownFrame > 0)
			{
				canInitiate = false;
			}
			else if(projectile.limitInstances > 0 && projectile.rexPool != null && projectile.rexPool.ActiveObjects() >= projectile.limitInstances)
			{
				canInitiate = false;
			}
			else if((isAttackActive || slots.actor.currentAttack != null) && !actionsAllowedDuringAttack.attacking)
			{
				canInitiate = false;
			}
			else if(!isEnabled)
			{
				canInitiate = false;
			}
			else if(slots.actor.slots.controller && (slots.actor.slots.controller.isKnockbackActive || slots.actor.slots.controller.isStunned || slots.actor.slots.controller.StateID() == DeathState.idString))
			{
				canInitiate = false;
			}
			else if(((stateID == DashState.idString && !canInitiateFrom.dashing) || (stateID == LadderState.idString && !canInitiateFrom.climbing) || (stateID == DefaultState.idString || stateID == LandingState.idString) && !canInitiateFrom.standing) || (stateID == MovingState.idString && !canInitiateFrom.moving) || ((stateID == JumpState.idString || stateID == FallingState.idString) && !canInitiateFrom.jumping) || (stateID == WallClingState.idString && !canInitiateFrom.wallClinging))
			{
				canInitiate = false;
			}
			else if(stateID == CrouchState.idString && !canInitiateFrom.crouching)
			{
				canInitiate = false;
			}

			return canInitiate;
		}

		//Checks to see if the attack is interruptable with other actions
		public bool CanInterrupt(ActionType _actionType)
		{
			bool canInterrupt = true;

			if((_actionType == ActionType.Dashing && !actionsAllowedDuringAttack.dashing) || (_actionType == ActionType.GroundMoving && !actionsAllowedDuringAttack.groundMoving) || (_actionType == ActionType.AirMoving && !actionsAllowedDuringAttack.airMoving) || (_actionType == ActionType.Jumping && !actionsAllowedDuringAttack.jumping) || (_actionType == ActionType.Turning && !actionsAllowedDuringAttack.turning) || (_actionType == ActionType.Climbing && !actionsAllowedDuringAttack.climbing) || (_actionType == ActionType.Crouching && !actionsAllowedDuringAttack.crouching) || (_actionType == ActionType.WallClinging && !actionsAllowedDuringAttack.wallClinging))
			{
				canInterrupt = false;
			}

			return canInterrupt;
		}

		//If a projectile is slotted, this spawns it
		public void CreateProjectile()
		{
			Direction.Horizontal direction = (slots.actor.slots.controller) ? slots.actor.slots.controller.direction.horizontal : Direction.Horizontal.Right;
			if(attackDirection == AttackDirection.Behind)
			{
				direction = (Direction.Horizontal)((int)direction * -1.0f);
			}

			Projectile newProjectile = projectile.rexPool.Spawn().GetComponent<Projectile>();
			Direction.Horizontal startingHorizontalDirection = direction;
			Direction.Vertical startingVerticalDirection = Direction.Vertical.Up;

			if(projectile.isAimable)
			{
				if(Mathf.Abs(slots.actor.slots.input.verticalAxis) == 1.0f && slots.actor.slots.input.horizontalAxis == 0.0f)
				{
					startingHorizontalDirection = Direction.Horizontal.Neutral;
				}

				startingVerticalDirection = (Direction.Vertical)(slots.actor.slots.input.verticalAxis * PhysicsManager.Instance.gravityScale);
			}

			newProjectile.isAimable = projectile.isAimable;
			newProjectile.Fire(new Vector2(slots.actor.transform.position.x + transform.localPosition.x * (int)direction, transform.position.y), startingHorizontalDirection, startingVerticalDirection, slots.actor, projectile.rexPool);
		}

		//This gets the animation clip that the actor using this attack should play based on this attack and the current State of the Controller
		public AnimationClip GetActorAnimationClip()
		{
			string stateID = slots.actor.slots.controller.StateID();
			AnimationClip animationClip = null;
			if(stateID == DefaultState.idString || stateID == LandingState.idString)
			{
				animationClip = actorAnimations.standing;
			}
			else if(stateID == MovingState.idString)
			{
				animationClip = actorAnimations.moving;
			}
			else if(stateID == JumpState.idString || stateID == FallingState.idString)
			{
				animationClip = actorAnimations.jumping;
			}
			else if(stateID == DashState.idString)
			{
				animationClip = actorAnimations.dashing;
			}
			else if(stateID == LadderState.idString)
			{
				animationClip = actorAnimations.climbing;
			}
			else if(stateID == CrouchState.idString)
			{
				animationClip = actorAnimations.crouching;
			}
			else if(stateID == WallClingState.idString)
			{
				animationClip = actorAnimations.wallClinging;
			}

			return animationClip;
		}

		#endregion

		#region private methods

		//This gets the animation clip that THIS ATTACK should play based on this attack and the current State of the Controller
		protected AnimationClip GetAttackAnimationClip()
		{
			string stateID = slots.actor.slots.controller.StateID();
			AnimationClip animationClip = null;
			if(stateID == DefaultState.idString || stateID == LandingState.idString)
			{
				animationClip = attackAnimations.standing;
			}
			else if(stateID == MovingState.idString)
			{
				animationClip = attackAnimations.moving;
			}
			else if(stateID == LadderState.idString)
			{
				animationClip = attackAnimations.climbing;
			}
			else if(stateID == DashState.idString)
			{
				animationClip = attackAnimations.dashing;
			}
			else if(stateID == JumpState.idString || stateID == FallingState.idString)
			{
				animationClip = attackAnimations.jumping;
			}
			else if(stateID == CrouchState.idString)
			{
				animationClip = attackAnimations.crouching;
			}
			else if(stateID == WallClingState.idString)
			{
				animationClip = attackAnimations.wallClinging;
			}

			return animationClip;
		}

		//Don't call this directly; this will be auto-called after a successful call to Begin()
		protected IEnumerator AttackCoroutine()
		{
			isAttackActive = true;
			slots.actor.currentAttack = this;
			currentCooldownFrame = cooldownFrames;

			if(slots.audio && audioClip)
			{
				slots.actor.PlaySoundIfOnCamera(audioClip, 1.0f, slots.audio);
			}

			if(slots.boxCollider != null && willAutoEnableCollider)
			{
				slots.boxCollider.enabled = true;
			}

			if(slots.spriteRenderer != null)
			{
				slots.spriteRenderer.enabled = true;
			}

			attackDirection = AttackDirection.Ahead;
			if(slots.actor.slots.controller.StateID() == WallClingState.idString)
			{
				WallClingState wallClingState = slots.actor.slots.controller.currentState.GetComponent<WallClingState>();
				if(wallClingState && wallClingState.attacksReverseOnWall)
				{
					attackDirection = AttackDirection.Behind;
				}
			}

			transform.localScale = (attackDirection == AttackDirection.Ahead) ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(-1.0f, 1.0f, 1.0f);

			AnimationClip attackAnimationClip = GetAttackAnimationClip();
			if(attackAnimationClip != null && slots.actor.slots.anim)
			{
				slots.animator.Play(attackAnimationClip.name, 0, 0.0f);
			}

			AnimationClip actorAnimationClip = GetActorAnimationClip();
			if(actorAnimationClip != null && slots.actor.slots.anim)
			{
				float timeToSyncTo = (slots.actor.slots.controller.StateID() == MovingState.idString) ? (float)slots.actor.slots.anim.GetCurrentAnimatorStateInfo(0).normalizedTime : 0.0f; //If the slots.actor is moving, sync the move-attack cycle to where their move cycle was

				if(willSyncMoveAnimation)
				{
					timeToSyncTo = 0.0f;
				}

				slots.actor.slots.anim.Play(actorAnimationClip.name, 0, timeToSyncTo);
			}

			if(projectile.rexPool != null)
			{
				CreateProjectile();
			}

			float durationWithoutAnimation = 0.5f;
			float duration = (attackAnimationClip != null) ? attackAnimationClip.length : durationWithoutAnimation;
			if(attackAnimationClip == null && actorAnimationClip != null)
			{
				duration = actorAnimationClip.length;
			}

			yield return new WaitForSeconds(duration);

			Reset();

			if(slots.actor != null)
			{
				slots.actor.OnAttackComplete();
			}
		}

		protected void SetCrouchPosition()
		{
			if(slots.actor.slots.controller.StateID() == CrouchState.idString)
			{
				transform.localPosition = new Vector3(nonCrouchingPosition.x, nonCrouchingPosition.y - crouchOffset, nonCrouchingPosition.z);
			}
			else
			{
				transform.localPosition = nonCrouchingPosition;
			}
		}

		//Cancels and resets the attack properties
		protected void Reset()
		{			
			StopCoroutine("AttackCoroutine");

			if(slots.boxCollider != null)
			{
				slots.boxCollider.enabled = false;
			}

			if(slots.spriteRenderer != null)
			{
				slots.spriteRenderer.enabled = false;
			}

			slots.actor.currentAttack = null;
			isAttackActive = false;
		}

		#endregion
	}	
}
