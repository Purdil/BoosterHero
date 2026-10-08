using System;
using UnityEngine;
using UnityEngine.InputSystem;


namespace _Project._Scripts.Core
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/PlayerInput")]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {

        public Action OnClickPointer;
        public Vector2 PointerDelta;
        
        private Controls _controls;
        
        private void OnEnable()
        {
            if(_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }

            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            if(_controls != null)
                _controls.Player.Disable();
        }
        
      

        public void OnDeltaPonter(InputAction.CallbackContext context)
        {
            PointerDelta = context.ReadValue<Vector2>();
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            if(context.performed)
                OnClickPointer?.Invoke();
        }
    }
}