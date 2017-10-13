/* Copyright Sky Tyrannosaur */

using UnityEngine;
using System.Collections;

namespace RexEngine
{
    public class RexInput : MonoBehaviour
    {
        protected const int playerId = 0;

        public bool isJumpButtonDown = false;
        public bool isJumpButtonDownThisFrame = false;
        public bool isDashButtonDown = false;
        public bool isRunButtonDown = false;
        public bool isSubAttackButtonDown = false;
        public bool isAttackButtonDown = false;
        public bool isSubAttackButtonDownThisFrame = false;
        public bool isAttackButtonDownThisFrame = false;
        public bool isAdvanceTextButtonDown = false;
        public bool isPauseButtonDown = false;

        public bool willAcceptInputWhenPaused;

        public float horizontalAxis;
        public float verticalAxis;
        public bool isEnabled = true;

        protected bool isKeyboardEnabled = true;

        public bool GetIsKeyboardEnabled()
        {
            return isKeyboardEnabled;
        }

        void Update()
        {
            SetInputs();
        }

        protected virtual void SetInputs()
        {
            if(isEnabled && Time.timeScale > 0 || willAcceptInputWhenPaused)
            {
                isAttackButtonDown = InputManager.Instance.GetButton(playerId, InputAction.Attack);
                isSubAttackButtonDown = InputManager.Instance.GetButton(playerId, InputAction.SubAttack);
                isAttackButtonDownThisFrame = InputManager.Instance.GetButtonDown(playerId, InputAction.Attack);
                isSubAttackButtonDownThisFrame = InputManager.Instance.GetButtonDown(playerId, InputAction.SubAttack);
                isJumpButtonDown = InputManager.Instance.GetButton(playerId, InputAction.Jump);
                isJumpButtonDownThisFrame = InputManager.Instance.GetButtonDown(playerId, InputAction.Jump);
                isDashButtonDown = InputManager.Instance.GetButtonDown(playerId, InputAction.Dash);
                isRunButtonDown = InputManager.Instance.GetButton(playerId, InputAction.Run);

                if(InputManager.Instance.GetAxis(playerId, InputAction.MoveHorizontal) > 0.5f)
                {
                    horizontalAxis = 1.0f;
                }
                else if(InputManager.Instance.GetAxis(playerId, InputAction.MoveHorizontal) < -0.5f)
                {
                    horizontalAxis = -1.0f;
                }
                else
                {
                    horizontalAxis = 0.0f;
                }

                if(InputManager.Instance.GetAxis(playerId, InputAction.MoveVertical) > 0.5f)
                {
                    verticalAxis = 1.0f;
                }
                else if(InputManager.Instance.GetAxis(playerId, InputAction.MoveVertical) < -0.5f)
                {
                    verticalAxis = -1.0f;
                }
                else
                {
                    verticalAxis = 0.0f;
                }
            }
            else
            {
                horizontalAxis = 0.0f;
                verticalAxis = 0.0f;
                isAttackButtonDown = false;
                isSubAttackButtonDown = false;
                isAttackButtonDownThisFrame = false;
                isSubAttackButtonDownThisFrame = false;
                isJumpButtonDown = false;
                isJumpButtonDownThisFrame = false;
                isPauseButtonDown = false;
                isDashButtonDown = false;
                isRunButtonDown = false;
            }

            if(isEnabled) //Pause can't take Time.timeScale into account, since it will always be 0 if the game is paused already
            {
                isPauseButtonDown = InputManager.Instance.GetButtonDown(playerId, InputAction.Pause);
            }
        }
    }
}
