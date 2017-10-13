/* Copyright Sky Tyrannosaur */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RexEngine
{
    public class UnityInputManager:InputManager
    {
        [SerializeField]
        protected string playerAxisPrefix = "";
        [SerializeField]
        protected int maxNumberOfPlayers = 1;

        [Header("Unity Axis Mappings")]
        [SerializeField]
        protected string jumpAxis = "Jump";
        [SerializeField]
        protected string attackAxis = "Fire1";
        [SerializeField]
        protected string subAttackAxis = "Fire2";
        [SerializeField]
        protected string dashAxis = "Fire3";
        [SerializeField]
        protected string runAxis = "Run";
        [SerializeField]
        protected string horizontalAxis = "Horizontal";
        [SerializeField]
        protected string verticalAxis = "Vertical";
        [SerializeField]
        protected string pauseAxis = "Submit";

        private Dictionary<int, string>[] actions;

        protected override void Awake()
        {
            base.Awake();
            
            //Do not override existing input sources so 3rd party managers can override this
            if(InputManager.Instance != null) 
			{
                Enabled = false; // disable this input manager to save resources
				return;
			}

            SetInstance(this); //Set this as the singleton instance

            //Set up Actions dictionary for each player
            actions = new Dictionary<int, string>[maxNumberOfPlayers];
            for(int i = 0; i < maxNumberOfPlayers; i++)
            {
                Dictionary<int, string> playerActions = new Dictionary<int, string>();
                actions[i] = playerActions;
                string prefix = !string.IsNullOrEmpty(playerAxisPrefix) ? playerAxisPrefix + i : string.Empty;
                AddAction(InputAction.Jump, prefix + jumpAxis, playerActions);
                AddAction(InputAction.Attack, prefix + attackAxis, playerActions);
                AddAction(InputAction.SubAttack, prefix + subAttackAxis, playerActions);
                AddAction(InputAction.Dash, prefix + dashAxis, playerActions);
                AddAction(InputAction.Run, prefix + runAxis, playerActions);
                AddAction(InputAction.MoveHorizontal, prefix + horizontalAxis, playerActions);
                AddAction(InputAction.MoveVertical, prefix + verticalAxis, playerActions);
                AddAction(InputAction.Pause, prefix + pauseAxis, playerActions);
            }
        }

        public override bool GetButton(int playerId, InputAction action)
        {
            bool value = Input.GetButton(actions[playerId][(int)action]);
            if(UseTouchInput) 
			{
				value |= TouchInputManager.GetButton(playerId, action);
			}

            return value;
        }

        public override bool GetButtonDown(int playerId, InputAction action)
        {
            bool value = Input.GetButtonDown(actions[playerId][(int)action]);
            if(UseTouchInput) 
			{
				value |= TouchInputManager.GetButtonDown(playerId, action);
			}

            return value;
        }

        public override bool GetButtonUp(int playerId, InputAction action)
        {
            bool value = Input.GetButtonUp(actions[playerId][(int)action]);
            if(UseTouchInput) 
			{
				value |= TouchInputManager.GetButtonUp(playerId, action);
			}

            return value;
        }

        public override float GetAxis(int playerId, InputAction action)
        {
            float value = Input.GetAxisRaw(actions[playerId][(int)action]);
            if(UseTouchInput)
            {
                float touchValue = TouchInputManager.GetAxis(playerId, action);
                if(Mathf.Abs(touchValue) > Mathf.Abs(value)) value = touchValue;
            }

            return value;
        }

        private static void AddAction(InputAction action, string actionName, Dictionary<int, string> actions)
        {
            if(string.IsNullOrEmpty(actionName)) 
			{
				return;
			}

            actions.Add((int)action, actionName);
        }
    }
}