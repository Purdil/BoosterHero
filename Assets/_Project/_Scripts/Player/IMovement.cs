using System;
using UnityEngine;

namespace _Project._Scripts.Player
{
    public enum BoostState
    {
        Ready,
        Active,
        Spent
    }

    public interface IMovement
    {
        event Action Landed;
        event Action<BoostState> BoostStateChanged;

        Vector3 Velocity { get; }
        bool IsGrounded { get; }
        bool IsBoosting { get; }
        bool MovementEnabled { get; }
        BoostState BoostState { get; }

        void SetSteering(float normalizedSteering);
        void StopHorizontalMotion();
        void SetBoostHeld(bool isHeld);
        void SetMovementEnabled(bool isEnabled);
        void Stop();
        void AddImpulse(Vector3 impulse);
    }
}
