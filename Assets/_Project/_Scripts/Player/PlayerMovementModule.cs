using System;
using System.Collections.Generic;
using _Project._Scripts.Core;
using _Project._Scripts.Traversal;
using DevLib.ModuleSystem;
using UnityEngine;

namespace _Project._Scripts.Player
{
    [DisallowMultipleComponent]
    public class PlayerMovementModule : MonoBehaviour, IModule, IMovement
    {
        private const float InputDeadZone = 0.02f;
        private const float VelocityEpsilon = 0.05f;
        private const int GroundContactCapacity = 8;
        private const int GroundProbeHitCapacity = 8;

        [Header("Vertical Movement")]
        [SerializeField, Min(0f)] private float gravityScale = 2.04f;
        [SerializeField, Min(0f)] private float groundedDownwardSpeed = 0.5f;
        [SerializeField, Min(0f)] private float boostAcceleration = 32f;
        [SerializeField, Min(0f)] private float maxRiseSpeed = 6.6f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 13f;

        [Header("Horizontal Movement")]
        [SerializeField, Min(0f)] private float horizontalAcceleration = 28f;
        [SerializeField, Min(0f)] private float maxHorizontalSpeed = 4f;
        [SerializeField, Min(0f)] private float horizontalDrag = 22f;
        [SerializeField, Min(1f)] private float reversingAccelerationMultiplier = 1.75f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Range(0f, 89f)] private float maxGroundAngle = 55f;
        [SerializeField, Range(0.1f, 1f)] private float groundProbeRadiusRatio = 0.7f;
        [SerializeField, Min(0f)] private float groundProbeDistance = 0.12f;
        [SerializeField, Min(0f)] private float groundProbeStartOffset = 0.02f;

        [Header("Tower Surface")]
        [SerializeField] private SurfaceAgent surfaceAgent;

        public event Action Landed;
        public event Action<BoostState> BoostStateChanged;

        public Vector3 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
        public bool IsGrounded => _isGrounded;
        public bool IsBoosting => _boostState == BoostState.Active;
        public bool MovementEnabled => _movementEnabled;
        public BoostState BoostState => _boostState;

        private Agent _owner;
        private Rigidbody _rigidbody;
        private Collider _collider;
        private IFuel _fuel;
        private PlayerGroundContactRelay _groundContactRelay;
        private readonly ContactPoint[] _groundContacts = new ContactPoint[GroundContactCapacity];
        private readonly RaycastHit[] _groundProbeHits = new RaycastHit[GroundProbeHitCapacity];
        private readonly HashSet<Collider> _groundColliders = new HashSet<Collider>();

        private float _steering;
        private bool _horizontalStopPending;
        private bool _boostHeld;
        private bool _movementEnabled = true;
        private bool _isGrounded;
        private BoostState _boostState = BoostState.Ready;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner as Agent;
            if (_owner == null)
            {
                Debug.LogError($"{nameof(PlayerMovementModule)} requires an {nameof(Agent)} owner.", this);
                enabled = false;
                return;
            }

            _rigidbody = _owner.GetComponent<Rigidbody>();
            _collider = _owner.GetComponent<Collider>();
            _fuel = owner.GetModule<IFuel>();
            if (surfaceAgent == null)
                surfaceAgent = _owner.GetComponent<SurfaceAgent>();

            if (_rigidbody == null || _collider == null || _fuel == null)
            {
                CLog.LogError(
                    $"{nameof(PlayerMovementModule)} requires Rigidbody, CapsuleCollider, and IFuel on its owner.",
                    this);
                enabled = false;
                return;
            }

            if (_rigidbody.useGravity)
                _rigidbody.useGravity = false;
            if (_rigidbody.interpolation == RigidbodyInterpolation.None)          
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            
            _rigidbody.useGravity = false;
            _groundContactRelay = _owner.GetComponent<PlayerGroundContactRelay>();
            if (_groundContactRelay == null)
            {
                CLog.LogError(
                    $"{nameof(PlayerMovementModule)} requires " +
                    $"{nameof(PlayerGroundContactRelay)} on its owner.",
                    this);

                enabled = false;
                return;
            }
            _groundContactRelay.Bind(this);
            SetGrounded(false);
        }

        private void OnDestroy()
        {
            if (_groundContactRelay != null)
                _groundContactRelay.Unbind(this);
        }

        private void FixedUpdate()
        {
            EvaluateGrounded();
            UpdateBoostState();
            ApplyMovement(Time.fixedDeltaTime);
        }

        internal void HandleCollisionEnter(Collision collision)
        {
            UpdateGroundContact(collision);
        }

        internal void HandleCollisionStay(Collision collision)
        {
            UpdateGroundContact(collision);
        }

        internal void HandleCollisionExit(Collision collision)
        {
            _groundColliders.Remove(collision.collider);
        }

        public void SetSteering(float normalizedSteering)
        {
            if (!_movementEnabled)
            {
                _steering = 0f;
                return;
            }
            
            _horizontalStopPending = false;
            _steering = Mathf.Clamp(normalizedSteering, -1f, 1f);
        }

        public void StopHorizontalMotion()
        {
            _steering = 0f;
            _horizontalStopPending = true;
        }

        public void SetBoostHeld(bool isHeld)
        {
            _boostHeld = _movementEnabled && isHeld;

            if (_boostHeld)
                return;

            if (_isGrounded)
            {
                SetBoostState(BoostState.Ready);
                return;
            }

            if (_boostState == BoostState.Active)
                SetBoostState(BoostState.Spent);
        }

        public void SetMovementEnabled(bool isEnabled)
        {
            if (_movementEnabled == isEnabled)
                return;

            _movementEnabled = isEnabled;
            if (_movementEnabled)
                return;

            _steering = 0f;
            _boostHeld = false;
            if (_boostState == BoostState.Active)
                SetBoostState(BoostState.Spent);
        }

        public void Stop()
        {
            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector3.zero;

            _owner?.Renderer?.SetMovementDirection(Vector2.zero);
            _steering = 0f;
            _horizontalStopPending = false;
            _boostHeld = false;
            Debug.Log($"Im Stop. Current Grounded: {_isGrounded}");
            SetBoostState(_isGrounded ? BoostState.Ready : BoostState.Spent);
        }

        public void AddImpulse(Vector3 impulse)
        {
            if (_rigidbody != null)
                _rigidbody.AddForce(impulse, ForceMode.Impulse);
        }

        private void UpdateBoostState()
        {
            if (!_movementEnabled)
                return;

            if (_boostState == BoostState.Ready && _boostHeld && _isGrounded && _fuel.HasFuel)
                SetBoostState(BoostState.Active);

            if (_boostState == BoostState.Active && (!_boostHeld || !_fuel.HasFuel))
                SetBoostState(BoostState.Spent);
        }

        private void ApplyMovement(float deltaTime)
        {
            if (surfaceAgent != null && surfaceAgent.IsBound)
                surfaceAgent.SynchronizePlanarPosition(_rigidbody.position);

            Vector3 up = Vector3.up;
            Vector3 tangent = surfaceAgent != null && surfaceAgent.IsBound
                ? surfaceAgent.GetPositiveTangent()
                : Vector3.ProjectOnPlane(_owner.transform.right, up).normalized;
            if (tangent.sqrMagnitude < Mathf.Epsilon)
                tangent = Vector3.right;

            Vector3 velocity = _rigidbody.linearVelocity;
            float verticalSpeed = Vector3.Dot(velocity, up);
            float horizontalSpeed = Vector3.Dot(velocity, tangent);
            Vector3 preservedVelocity = velocity - up * verticalSpeed - tangent * horizontalSpeed;

            bool isCompletingCorner = surfaceAgent != null
                                      && surfaceAgent.IsBound
                                      && surfaceAgent.IsTransitioning;
            if (_horizontalStopPending && !isCompletingCorner)
            {
                horizontalSpeed = 0f;
                _horizontalStopPending = false;
            }
            else
            {
                horizontalSpeed = CalculateHorizontalSpeed(horizontalSpeed, deltaTime);
            }

            bool isLaunching = _boostState == BoostState.Active;
            float gravityAcceleration = Vector3.Dot(Physics.gravity, up) * gravityScale;
            verticalSpeed += gravityAcceleration * deltaTime;

            if (_isGrounded && !isLaunching)
                verticalSpeed = Mathf.Min(verticalSpeed, -groundedDownwardSpeed);

            if (_boostState == BoostState.Active)
            {
                bool consumedFuel = _fuel.TryConsume(_fuel.ConsumptionPerSecond * deltaTime);
                if (consumedFuel)
                    verticalSpeed += boostAcceleration * deltaTime;

                if (!_fuel.HasFuel)
                    SetBoostState(BoostState.Spent);
            }

            verticalSpeed = Mathf.Clamp(verticalSpeed, -maxFallSpeed, maxRiseSpeed);

            if (surfaceAgent != null && surfaceAgent.IsBound)
            {
                Vector3 targetPosition = surfaceAgent.Advance(
                    horizontalSpeed,
                    deltaTime,
                    _rigidbody.position.y);
                Vector3 horizontalDelta = Vector3.ProjectOnPlane(
                    targetPosition - _rigidbody.position,
                    up);

                // v = deltaPosition / deltaTime. SurfaceAgent decides the exact point
                // on the straight face or corner arc; converting its displacement back
                // to velocity keeps this module as the Rigidbody's single velocity writer.
                Vector3 constrainedVelocity = deltaTime > Mathf.Epsilon
                    ? horizontalDelta / deltaTime
                    : Vector3.zero;
                _rigidbody.linearVelocity = constrainedVelocity + up * verticalSpeed;
            }
            else
            {
                _rigidbody.linearVelocity = preservedVelocity
                                            + tangent * horizontalSpeed
                                            + up * verticalSpeed;
            }

            ReportMovementDirection(horizontalSpeed, verticalSpeed);
        }

        private void ReportMovementDirection(float horizontalSpeed, float verticalSpeed)
        {
            if (_owner?.Renderer == null)
                return;

            float normalizedHorizontal = maxHorizontalSpeed > Mathf.Epsilon
                ? horizontalSpeed / maxHorizontalSpeed
                : 0f;
            float verticalLimit = verticalSpeed >= 0f ? maxRiseSpeed : maxFallSpeed;
            float normalizedVertical = verticalLimit > Mathf.Epsilon
                ? verticalSpeed / verticalLimit
                : 0f;

            _owner.Renderer.SetMovementDirection(new Vector2(
                Mathf.Clamp(normalizedHorizontal, -1f, 1f),
                Mathf.Clamp(normalizedVertical, -1f, 1f)));
        }

        private float CalculateHorizontalSpeed(float currentSpeed, float deltaTime)
        {
            if (!_movementEnabled || Mathf.Abs(_steering) <= InputDeadZone)
                return Mathf.MoveTowards(currentSpeed, 0f, horizontalDrag * deltaTime);

            bool isReversing = Mathf.Abs(currentSpeed) > VelocityEpsilon
                               && !Mathf.Approximately(Mathf.Sign(_steering), Mathf.Sign(currentSpeed));
            float acceleration = horizontalAcceleration
                                 * (isReversing ? reversingAccelerationMultiplier : 1f);
            currentSpeed += _steering * acceleration * deltaTime;
            return Mathf.Clamp(currentSpeed, -maxHorizontalSpeed, maxHorizontalSpeed);
        }

        private void UpdateGroundContact(Collision collision)
        {
            Collider otherCollider = collision.collider;
            if (otherCollider == null || !IsInGroundMask(otherCollider.gameObject.layer))
            {
                _groundColliders.Remove(otherCollider);
                return;
            }

            int contactCount = collision.GetContacts(_groundContacts);
            bool hasValidGroundContact = false;
            float minimumGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            for (int i = 0; i < contactCount; i++)
            {
                ContactPoint contact = _groundContacts[i];
                bool capsuleIsThisCollider = contact.thisCollider == _collider;
                bool capsuleIsOtherCollider = contact.otherCollider == _collider;
                if (!capsuleIsThisCollider && !capsuleIsOtherCollider)
                    continue;

                Vector3 contactNormal = capsuleIsThisCollider ? contact.normal : -contact.normal;
                if (Vector3.Dot(contactNormal, Vector3.up) >= minimumGroundDot)
                {
                    hasValidGroundContact = true;
                    break;
                }
            }

            if (hasValidGroundContact)
                _groundColliders.Add(otherCollider);
            else
                _groundColliders.Remove(otherCollider);
        }

        private void EvaluateGrounded()
        {
            _groundColliders.RemoveWhere(collider => collider == null);

            bool hasWalkableContact = _groundColliders.Count > 0;
            bool isGrounded = hasWalkableContact && CheckFootSupport();
            SetGrounded(isGrounded);
        }

        private bool CheckFootSupport()
        {
            Bounds bounds = _collider.bounds;
            float colliderRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
            if (colliderRadius <= Mathf.Epsilon)
                return false;

            float probeRadius = colliderRadius * groundProbeRadiusRatio;
            Vector3 probeOrigin = new Vector3(
                bounds.center.x,
                bounds.min.y + probeRadius + groundProbeStartOffset,
                bounds.center.z);
            float probeDistance = groundProbeStartOffset + groundProbeDistance;
            int hitCount = Physics.SphereCastNonAlloc(
                probeOrigin,
                probeRadius,
                Vector3.down,
                _groundProbeHits,
                probeDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            float minimumGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _groundProbeHits[i];
                if (hit.collider == null || hit.collider == _collider)
                    continue;

                if (Vector3.Dot(hit.normal, Vector3.up) >= minimumGroundDot)
                    return true;
            }

            return false;
        }

        private bool IsInGroundMask(int layer)
        {
            return (groundMask.value & (1 << layer)) != 0;
        }

        private void SetGrounded(bool grounded)
        {
            if (_isGrounded == grounded)
            {
                if (grounded && _movementEnabled && !_boostHeld && _boostState == BoostState.Spent)
                    SetBoostState(BoostState.Ready);

                return;
            }

            bool wasGrounded = _isGrounded;
            _isGrounded = grounded;

            if (!wasGrounded && _isGrounded)
            {
                _fuel.Refill();

                SetBoostState(BoostState.Ready);
                Landed?.Invoke();
            }
            else if (wasGrounded && !_isGrounded && _boostState == BoostState.Ready)
            {
                SetBoostState(BoostState.Spent);
            }
        }

        private void SetBoostState(BoostState nextState)
        {
            if (_boostState == nextState)
                return;

            _boostState = nextState;
            BoostStateChanged?.Invoke(_boostState);
        }
    }
}
