using _Project._Scripts.Core;
using DevLib.ModuleSystem;
using UnityEngine;

namespace _Project._Scripts.Player
{
    [DisallowMultipleComponent]
    public class PlayerControlModule : Module
    {
        [SerializeField] private PlayerInputSO inputSO;
        [SerializeField, Range(0.1f, 1f)] private float fullSteerScreenRatio = 0.34f;
        [SerializeField, Min(1f)] private float minimumFullSteerPixels = 80f;

        private IMovement _movement;
        private Vector2 _pressStartPosition;
        private bool _initialized;
        private bool _subscribed;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _movement = owner.GetModule<IMovement>();
            _initialized = true;

            if (inputSO == null || _movement == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerControlModule)} requires a PlayerInputSO and an IMovement module.",
                    this);
                enabled = false;
                return;
            }

            SubscribeInput();
            ResetMovementIntent();
        }

        private void OnEnable()
        {
            if (_initialized)
                SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            ResetMovementIntent();
        }

        private void Update()
        {
            if (_movement == null || inputSO == null || !inputSO.IsPointerHeld)
                return;

            float fullSteerDistance = Mathf.Max(
                minimumFullSteerPixels,
                Screen.width * fullSteerScreenRatio);
            float dragDistance = inputSO.PointerPosition.x - _pressStartPosition.x;
            float steering = Mathf.Clamp(dragDistance / fullSteerDistance, -1f, 1f);

            _movement.SetSteering(steering);
            _movement.SetBoostHeld(true);
        }

        private void HandlePointerPressed()
        {
            _pressStartPosition = inputSO.PointerPosition;
            _movement.SetSteering(0f);
            _movement.SetBoostHeld(true);
        }

        private void HandlePointerReleased()
        {
            ResetMovementIntent();
        }

        private void SubscribeInput()
        {
            if (_subscribed || inputSO == null)
                return;

            inputSO.PointerPressed += HandlePointerPressed;
            inputSO.PointerReleased += HandlePointerReleased;
            _subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_subscribed || inputSO == null)
                return;

            inputSO.PointerPressed -= HandlePointerPressed;
            inputSO.PointerReleased -= HandlePointerReleased;
            _subscribed = false;
        }

        private void ResetMovementIntent()
        {
            if (_movement == null)
                return;

            _movement.StopHorizontalMotion();
            _movement.SetBoostHeld(false);
        }
    }
}
