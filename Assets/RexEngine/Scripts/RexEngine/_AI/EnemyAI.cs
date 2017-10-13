/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class EnemyAI:RexAI 
	{
		[System.Serializable]
		public class Slots
		{
			public RexController controller;
			public RexPhysics physicsObject;
			public BoxCollider2D boxCollider;
		}

		[System.Serializable]
		public class StartingMovement
		{
			public Direction.Horizontal horizontal;
			public Direction.Vertical vertical;
			public bool willFacePlayerAtStart = true;
		}

		[System.Serializable]
		public class MoveTowardsTransform
		{
			public Transform transformToMoveTowards;
			public bool usePlayerAsTarget;
			public bool willMoveTowardsX;
			public bool willMoveTowardsY;
			public float buffer = 0.5f;
			public bool isEnabled = true;
		}

		[System.Serializable]
		public class Turn
		{
			public bool onWallContact = true;
			public bool onCeilingFloorContact = false;
			public bool onLedgeContact = true;
		}

		[System.Serializable]
		public class Jump
		{
			[HideInInspector]
			public JumpState jumpState;
			public bool onLedges;
			public bool willAutoJumpAtIntervals = true;
			public int framesBetweenJumps = 128;

			[HideInInspector]
			public int currentFrameBetweenJumps;
		}

		public Slots slots;
		public StartingMovement startingMovement;
		public Turn turn;
		public Jump jump;
		public MoveTowardsTransform moveTowards;
		public Attacks attacks;

		[System.Serializable]
		public class Attacks
		{
			public Attack attackToPerform;
			public bool willAutoAttackAtIntervals = true;
			public int minFramesBetweenAttacks;
			public int maxFramesBetweenAttacks;

			[HideInInspector]
			public int currentFrameBetweenAttacks;
		}

		void Awake() 
		{
			if(jump.jumpState == null)
			{
				jump.jumpState = GetComponent<JumpState>();
			}
		}

		void Start() 
		{
			if(moveTowards.willMoveTowardsX || moveTowards.willMoveTowardsY)
			{
				if(moveTowards.isEnabled)
				{
					moveTowards.transformToMoveTowards = GameManager.Instance.player.transform;
				}
			}

			if(startingMovement.willFacePlayerAtStart)
			{
				FacePlayer();
				if(GameManager.Instance.player) //If we turn to face something, make sure our startingDirection is set to the direction we face
				{
					if(startingMovement.horizontal != Direction.Horizontal.Neutral)
					{
						if(GameManager.Instance.player.transform.position.x < transform.position.x)
						{
							startingMovement.horizontal = Direction.Horizontal.Left;
						}
						else
						{
							startingMovement.horizontal = Direction.Horizontal.Right;
						}
					}

					if(startingMovement.vertical != Direction.Vertical.Neutral)
					{
						if(GameManager.Instance.player.transform.position.y < transform.position.y)
						{
							startingMovement.vertical = Direction.Vertical.Down;
						}
						else
						{
							startingMovement.vertical = Direction.Vertical.Up;
						}
					}

					if(moveTowards.usePlayerAsTarget)
					{
						moveTowards.transformToMoveTowards = GameManager.Instance.player.transform;
					}
				}
			}

			if(slots.controller)
			{
				slots.controller.SetAxis(new Vector2((int)startingMovement.horizontal, (int)startingMovement.vertical));
			}

			attacks.currentFrameBetweenAttacks = RexMath.RandomInt(attacks.minFramesBetweenAttacks, attacks.maxFramesBetweenAttacks);
			jump.currentFrameBetweenJumps = jump.framesBetweenJumps;
		}

		void FixedUpdate()
		{
			if(!(slots.controller && slots.controller.StateID() != DeathState.idString && !slots.controller.isKnockbackActive && Time.timeScale > 0.0f))
			{
				return;
			}

			if(!slots.controller || !slots.physicsObject)
			{
				return;
			}

			if(moveTowards.transformToMoveTowards != null)
			{
				MoveTowards(moveTowards.transformToMoveTowards);
			}

			//If this has a target to move towards, don't turn around on wall or ledge contact
			if(!(moveTowards.transformToMoveTowards != null && (moveTowards.willMoveTowardsX || moveTowards.willMoveTowardsY)))
			{
				CheckForLedges();
				TurnOnWallContact();
			}

			if(jump.willAutoJumpAtIntervals)
			{
				CheckForAutoJumps();
			}

			if(attacks.willAutoAttackAtIntervals)
			{
				CheckForAttacks();
			}
		}

		public void OnNewStateAdded(RexState _state)
		{
			if(_state.id == JumpState.idString && !jump.jumpState)
			{
				jump.jumpState = _state as JumpState;
			}
		}

		public void FacePlayer()
		{
			if(GameManager.Instance.player != null && slots.controller)
			{
				if(GameManager.Instance.player.transform.position.x < transform.position.x)
				{
					slots.controller.FaceDirection(Direction.Horizontal.Left);
				}
				else
				{
					slots.controller.FaceDirection(Direction.Horizontal.Right);
				}
			}
		}

		protected void CheckForLedges()
		{
			if(jump.onLedges || turn.onLedgeContact)
			{
				if(slots.controller.StateID() == LadderState.idString)
				{
					return;
				}

				if(slots.controller.axis.x != 0.0f && slots.physicsObject.IsOnSurface() && RaycastHelper.IsNextToLedge((Direction.Horizontal)slots.controller.axis.x, (Direction.Vertical)(slots.controller.GravityScaleMultiplier() * -1.0f), slots.boxCollider))
				{
					if(jump.onLedges && !slots.physicsObject.IsAgainstEitherWall())
					{
						if(jump.jumpState)
						{
							jump.jumpState.Begin();
						}
					}
					else if(turn.onLedgeContact && !slots.physicsObject.IsAgainstEitherWall())
					{
						slots.controller.SetAxis(new Vector2(slots.controller.axis.x * -1.0f, slots.controller.axis.y));
					}
				}
			}
		}

		protected void CheckForAutoJumps()
		{
			if(jump.jumpState == null)
			{
				jump.jumpState = GetComponent<JumpState>();
			}

			if(jump.jumpState != null)
			{
				jump.currentFrameBetweenJumps --;
				if(jump.currentFrameBetweenJumps <= 0)
				{
					jump.currentFrameBetweenJumps = jump.framesBetweenJumps;
					jump.jumpState.Begin();
				}
			}
		}

		protected void CheckForAttacks()
		{
			if(attacks.attackToPerform != null)
			{
				attacks.currentFrameBetweenAttacks --;
				if(attacks.currentFrameBetweenAttacks <= 0)
				{
					attacks.currentFrameBetweenAttacks = RexMath.RandomInt(attacks.minFramesBetweenAttacks, attacks.maxFramesBetweenAttacks);
					attacks.attackToPerform.Begin();
				}
			}
		}

		protected void TurnOnWallContact()
		{
			if(slots.controller)
			{
				if(slots.controller.StateID() == LadderState.idString)
				{
					return;
				}

				if(turn.onWallContact && ((slots.controller.axis.x < 0.0f && slots.physicsObject.properties.isAgainstLeftWall) || (slots.controller.axis.x > 0.0f && slots.physicsObject.properties.isAgainstRightWall)))
				{
					slots.controller.SetAxis(new Vector2(slots.controller.axis.x * -1.0f, slots.controller.axis.y));
				}
				else if(turn.onCeilingFloorContact && ((slots.controller.axis.y < 0.0f && slots.physicsObject.properties.isGrounded) || (slots.controller.axis.y > 0.0f && slots.physicsObject.properties.isAgainstCeiling)))
				{
					slots.controller.SetAxis(new Vector2(slots.controller.axis.x, slots.controller.axis.y * -1.0f));
				}
			}
		}

		protected void MoveTowards(Transform _transform)
		{
			if(moveTowards.willMoveTowardsX && _transform != null)
			{
				Transform player = GameManager.Instance.player.transform;
				if(player)
				{
					if(player.transform.position.x > transform.position.x + moveTowards.buffer)
					{
						slots.controller.SetAxis(new Vector2(1.0f, slots.controller.axis.y));
					}
					else if(player.transform.position.x < transform.position.x - moveTowards.buffer)
					{
						slots.controller.SetAxis(new Vector2(-1.0f, slots.controller.axis.y));
					}
					else
					{
						slots.controller.SetAxis(new Vector2(0.0f, slots.controller.axis.y));
					}
				}
			}

			if(moveTowards.willMoveTowardsY && _transform != null)
			{
				Transform player = GameManager.Instance.player.transform;
				float adjustedBuffer = (slots.controller.StateID() == LadderState.idString) ? -0.5f : moveTowards.buffer;
				if(player)
				{
					LadderState ladderState = slots.controller.GetComponent<LadderState>();
					if(slots.controller.StateID() == LadderState.idString)
					{
						if(player.transform.position.y > transform.position.y + moveTowards.buffer || (ladderState.GetDistanceFromTop() < 1.5f && slots.controller.axis.y > 0.0f))
						{
							slots.controller.SetAxis(new Vector2(slots.controller.axis.x, 1.0f));
						}
						else if(player.transform.position.y < transform.position.y - moveTowards.buffer || (ladderState.GetDistanceFromBottom() < 1.5f && slots.controller.axis.y < 0.0f))
						{
							slots.controller.SetAxis(new Vector2(slots.controller.axis.x, -1.0f));
						}
						else
						{
							if(ladderState.GetDistanceFromTop() > 1.5f && ladderState.GetDistanceFromBottom() > 1.5f)
							{
								slots.controller.SetAxis(new Vector2(slots.controller.axis.x, 0.0f));
							}
						}
					}
					else
					{
						if(player.transform.position.y > transform.position.y + moveTowards.buffer)
						{
							slots.controller.SetAxis(new Vector2(slots.controller.axis.x, 1.0f));
						}
						else if(player.transform.position.y < transform.position.y - moveTowards.buffer)
						{
							slots.controller.SetAxis(new Vector2(slots.controller.axis.x, -1.0f));
						}
						else
						{
							slots.controller.SetAxis(new Vector2(slots.controller.axis.x, 0.0f));
						}
					}
				}
			}
		}
	}
}
