using UnityEngine;

namespace _Project._Scripts.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerGroundContactRelay : MonoBehaviour
    {
        private PlayerMovementModule _listener;

        internal void Bind(PlayerMovementModule listener)
        {
            _listener = listener;
        }

        internal void Unbind(PlayerMovementModule listener)
        {
            if (_listener == listener)
                _listener = null;
        }

        private void OnCollisionEnter(Collision collision)
        {
            _listener?.HandleCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            _listener?.HandleCollisionStay(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            _listener?.HandleCollisionExit(collision);
        }
    }
}