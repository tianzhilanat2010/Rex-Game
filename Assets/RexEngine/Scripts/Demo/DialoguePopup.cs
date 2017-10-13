/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
	public class DialoguePopup:MonoBehaviour 
	{
		public enum DialogueType
		{
			Intro,
			CallToAction,
			EnemyAbility,
			ReflectBullet,
			Gravity,
			Moveset,
			Ladder
		}

		public DialogueType dialogueType;
		public bool willAutoShow = false;

		void Awake() 
		{

		}

		void Start() 
		{
			if(willAutoShow)
			{
				GetComponent<BoxCollider2D>().enabled = false;
				DialogueManager.Instance.Show(GetDialogue());
			}
		}

		protected string GetDialogue()
		{
			string description = "";
			switch(dialogueType)
			{
				case DialogueType.Intro:
					description = "Welcome to Booster’s Adventure! This short \ngame will show you some of the things you \ncan do with Rex Engine. Every mechanic you’ll \nsee here comes out-of-the-box!";
					break;
				case DialogueType.CallToAction:
					description = "Our hero, Booster the t-rex, is an inventor \nand a pilot! Right now, he’s in a jam: he’s lost \nthe blueprints for his latest invention, \nand he needs your help to get them back!";
					break;
				case DialogueType.EnemyAbility:
					description = "Rex Engine also makes it easy to give enemies \nany and all of the abilities you can give the \nplayer. These enemies are thrilled with their\n newfound ability to jump. Watch out!";
					break;
				case DialogueType.ReflectBullet:
					description = "Rex Engine gives you both projectiles and \nthe option to make attacks deflect them. \nTry attacking the bullets this miniboss fires \nat you!";
					break;
				case DialogueType.Gravity:
					description = "With Rex Engine, you can alter gravity on \na whim. You can even lower \ngravity to give your game moon physics!";
					break;
				case DialogueType.Moveset:
					description = "Rex Engine lets you create multiple movesets \nfor your characters and switch between \nthem on the fly. Here, we’ve given Booster \na completely unique moveset for swimming!";
					break;
				case DialogueType.Ladder:
					description = "With Rex Engine, \nenemies can climb ladders too! They can do \neverything the player can!";
					break;
				default:
					description = "Welcome to Booster’s Adventure! This short game will show you some of the things you can do with Rex Engine. Every mechanic you’ll see here comes out-of-the-box!";
					break;
			}

			return description;
		}

		protected void OnTriggerEnter2D(Collider2D col)
		{
			if(col.tag == "Player")
			{
				GetComponent<BoxCollider2D>().enabled = false;
				DialogueManager.Instance.Show(GetDialogue());
			}
		}
	}

}
