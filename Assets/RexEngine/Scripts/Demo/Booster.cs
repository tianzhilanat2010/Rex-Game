/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RexEngine;

public class Booster:RexActor 
{
	public Attack peaShooterAttack;
	public Attack flyingPeaShooterAttack;
	public Attack meleeAttack;
	public AnimationClip victoryAnimation;
	public RexController flyingController;
	public RexPool growWingsPool;
	public AudioClip growWingsSound;

	void Start() 
	{
		DontDestroyOnLoad(gameObject);
	}

	public void OnBlueprintFound()
	{
		slots.controller.SetAxis(new Vector2(0, 0));
		slots.physicsObject.SetVelocityX(0.0f);
		slots.controller.isEnabled = false;
		slots.anim.Play(victoryAnimation.name, 0, 0);
	}

	public void SetToFlyingController()
	{
		SetController(flyingController);
		GameObject growWingsParticle = growWingsPool.Spawn();
		growWingsParticle.transform.localPosition = new Vector3(0, 0, 0);
		growWingsParticle.GetComponent<RexParticle>().Play();
	}

	public void SetToRegularController()
	{
		SetController(waterProperties.landController);
	}

	public override void Reset()
	{
		GameManager.Instance.player.GetComponent<Booster>().SetToRegularController();

		if(!DataManager.Instance.hasUnlockedBounce)
		{
			GameManager.Instance.player.slots.controller.GetComponent<BounceState>().isEnabled = false;
		}

		if(!DataManager.Instance.hasUnlockedProjectile)
		{
			GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Charge").GetComponent<Attack>().isEnabled = false;
		}

		if(!DataManager.Instance.hasUnlockedDoubleJump)
		{
			GameManager.Instance.player.slots.controller.GetComponent<JumpState>().multipleJumpNumber = 1;
		}

		if(!DataManager.Instance.hasUnlockedWallCling)
		{
			GameManager.Instance.player.slots.controller.GetComponent<WallClingState>().isEnabled = false;
		}

		if(!DataManager.Instance.hasUnlockedFly)
		{
			GameManager.Instance.player.slots.physicsObject.gravitySettings.usesGravity = true;

			GameManager.Instance.player.transform.Find("Attacks").Find("Melee").GetComponent<Attack>().isEnabled = true;
			GameManager.Instance.player.transform.Find("Attacks").Find("PeaShooter_Flying").GetComponent<Attack>().isEnabled = false;
		}
	}

	protected override void OnControllerChanged(RexController _newController)
	{
		if(_newController == waterProperties.landController)
		{
			meleeAttack.isEnabled = true;
			GetComponent<BoxCollider2D>().size = new Vector2(1.0f, 1.83f);
		}
		else if(_newController == waterProperties.waterController)
		{
			meleeAttack.isEnabled = false;
			GetComponent<BoxCollider2D>().size = new Vector2(2.3f, 1.83f);
		}
		else if(_newController == flyingController)
		{
			GetComponent<BoxCollider2D>().size = new Vector2(2.3f, 1.25f);
			flyingPeaShooterAttack.isEnabled = true;
			peaShooterAttack.isEnabled = false;
			meleeAttack.isEnabled = false;
			PlaySoundIfOnCamera(growWingsSound);
		}
	}
}
