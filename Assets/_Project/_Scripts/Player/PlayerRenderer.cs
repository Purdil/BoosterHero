using System;
using _Project._Scripts.Traversal;
using DevLib.AnimatorSystem;
using DevLib.ModuleSystem;
using UnityEngine;

namespace _Project._Scripts.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerRenderer : Module, IAnimatorRenderer
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Visual Hierarchy")]
        [SerializeField] private SurfaceAgent surfaceAgent;
        [SerializeField] private Transform visualFacingRoot;
        [SerializeField] private Transform leanPivot;

        [Header("Horizontal Lean")]
        [SerializeField, Range(0f, 45f)] private float maxLeanAngle = 14f;
        [SerializeField, Min(0.01f)] private float leanSmoothTime = 0.08f;

        public Animator Animator => animator;

        public event Action OnAnimationEnd;
        public event Action OnDamageCast;
        public event Action<Vector2> OnFootstep;

        private Vector2 _movementDirection;
        private Quaternion _initialLeanRotation;
        private float _currentLeanAngle;
        private float _leanAngularVelocity;
        private float _transitionLeanAngle;
        private bool _wasTransitioning;
        private bool _isInitialized;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            if (surfaceAgent == null)
                surfaceAgent = owner.GetComponent<SurfaceAgent>();

            if (visualFacingRoot == null)
                visualFacingRoot = transform;

            if (surfaceAgent == null || leanPivot == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerRenderer)} requires a {nameof(SurfaceAgent)} and a lean pivot.",
                    this);
                enabled = false;
                return;
            }

            _initialLeanRotation = leanPivot.localRotation;
            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
                return;

            SurfaceTraversalSnapshot snapshot = surfaceAgent.GetRenderSnapshot();
            if (snapshot.Surface != null)
            {
                float viewYaw = snapshot.Surface.EvaluateViewYaw(snapshot);
                visualFacingRoot.rotation = Quaternion.Euler(0f, viewYaw, 0f);
            }

            if (snapshot.IsTransitioning && !_wasTransitioning)
                _transitionLeanAngle = _currentLeanAngle;

            float targetLeanAngle = snapshot.IsTransitioning
                ? _transitionLeanAngle
                : Mathf.Clamp(
                    -_movementDirection.x * maxLeanAngle,
                    -maxLeanAngle,
                    maxLeanAngle);

            _currentLeanAngle = Mathf.SmoothDampAngle(
                _currentLeanAngle,
                targetLeanAngle,
                ref _leanAngularVelocity,
                leanSmoothTime);
            _currentLeanAngle = Mathf.Clamp(
                Mathf.DeltaAngle(0f, _currentLeanAngle),
                -maxLeanAngle,
                maxLeanAngle);

            leanPivot.localRotation = _initialLeanRotation
                                      * Quaternion.AngleAxis(
                                          _currentLeanAngle,
                                          Vector3.forward);
            _wasTransitioning = snapshot.IsTransitioning;
        }

        public void RenderClip(int clipHash)
        {
            if (animator == null || clipHash == 0)
                return;

            animator.Play(clipHash, 0, 0f);
        }

        public void RenderClipIfNotPlaying(int clipHash)
        {
            if (animator == null || clipHash == 0 || IsPlaying(clipHash))
                return;

            RenderClip(clipHash);
        }

        public void SetMovementDirection(Vector2 dir)
        {
            _movementDirection = new Vector2(
                Mathf.Clamp(dir.x, -1f, 1f),
                Mathf.Clamp(dir.y, -1f, 1f));
        }

        public Vector2 GetFacingDirection()
        {
            return _movementDirection;
        }

        public void NotifyAnimationEnd()
        {
            OnAnimationEnd?.Invoke();
        }

        public void NotifyDamageCast()
        {
            OnDamageCast?.Invoke();
        }

        public void NotifyFootstep(Vector2 direction)
        {
            OnFootstep?.Invoke(direction);
        }

        private bool IsPlaying(int clipHash)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (Matches(current, clipHash))
                return true;

            return animator.IsInTransition(0)
                   && Matches(animator.GetNextAnimatorStateInfo(0), clipHash);
        }

        private static bool Matches(AnimatorStateInfo state, int clipHash)
        {
            return state.shortNameHash == clipHash || state.fullPathHash == clipHash;
        }
    }
}
