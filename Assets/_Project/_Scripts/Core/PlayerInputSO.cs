using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project._Scripts.Core
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/PlayerInput")]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public event Action PointerPressed;
        public event Action PointerReleased;

        public Vector2 PointerPosition { get; private set; }
        public bool IsPointerHeld { get; private set; }

        private Controls _controls;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }

            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            ReleasePointer();

            if (_controls != null)
                _controls.Player.Disable();
        }

        public void OnPointerPosition(InputAction.CallbackContext context)
        {
            PointerPosition = context.ReadValue<Vector2>();
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (IsPointerHeld)
                    return;

                PointerPosition = ReadPointerPosition(context);
                IsPointerHeld = true;
                PointerPressed?.Invoke();
            }
            else if (context.canceled)
            {
                ReleasePointer();
            }
        }

        private void ReleasePointer()
        {
            if (!IsPointerHeld)
                return;

            IsPointerHeld = false;
            PointerReleased?.Invoke();
        }

        private Vector2 ReadPointerPosition(InputAction.CallbackContext context)
        {
            if (context.control?.device is Touchscreen touchscreen)
                return touchscreen.primaryTouch.position.ReadValue();

            if (context.control?.device is Mouse mouse)
                return mouse.position.ReadValue();

            return _controls.Player.PointerPosition.ReadValue<Vector2>();
        }
    }
}
