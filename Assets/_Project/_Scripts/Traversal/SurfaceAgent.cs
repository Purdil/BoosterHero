using UnityEngine;

namespace _Project._Scripts.Traversal
{
    [DisallowMultipleComponent]
    public sealed class SurfaceAgent : MonoBehaviour
    {
        [SerializeField] private TowerSurface surface;
        [SerializeField, Range(0, 3)] private int initialFace;
        [SerializeField, Min(0f)] private float edgeInset;
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.4f;

        private int _currentFace;
        private float _horizontal;
        private bool _isTransitioning;
        private int _fromFace;
        private int _toFace;
        private int _direction;
        private float _transitionTime;
        private float _previousProgress;
        private float _currentProgress;
        private float _resolvedSurfaceOffset;

        //외부 UI 등의 참조 프로퍼티
        public TowerSurface Surface => surface;
        public int CurrentFace => _currentFace;
        public float Horizontal => _horizontal;
        public bool IsBound => surface != null;
        public bool IsTransitioning => _isTransitioning;
        public float ResolvedSurfaceOffset => _resolvedSurfaceOffset;

        private void Awake()
        {
            float horizontal = surface == null
                ? 0f
                : Vector3.Dot(
                    transform.position - surface.Center,
                    surface.GetFaceTangent(initialFace));
            Place(surface, initialFace, horizontal);
        }

        public void Place(TowerSurface targetSurface, int faceIndex, float horizontal)
        {
            surface = targetSurface;
            _currentFace = NormalizeFace(faceIndex);
            _resolvedSurfaceOffset = ResolveSurfaceOffset();
            _horizontal = surface == null
                ? horizontal
                : Mathf.Clamp(horizontal, -GetEdgeLimit(), GetEdgeLimit());
            _isTransitioning = false;
            _fromFace = _currentFace;
            _toFace = _currentFace;
            _direction = 0;
            _transitionTime = 0f;
            _previousProgress = 0f;
            _currentProgress = 0f;

            if (surface != null)
            {
                transform.position = surface.SurfaceToWorld(
                    _currentFace,
                    _horizontal,
                    transform.position.y,
                    _resolvedSurfaceOffset);
            }
        }

        public void SynchronizePlanarPosition(Vector3 worldPosition)
        {
            if (surface == null || _isTransitioning)
                return;

            Vector3 tangent = surface.GetFaceTangent(_currentFace);
            float actualHorizontal = Vector3.Dot(worldPosition - surface.Center, tangent);
            _horizontal = Mathf.Clamp(actualHorizontal, -GetEdgeLimit(), GetEdgeLimit());
        }

        public Vector3 Advance(float horizontalSpeed, float deltaTime, float worldHeight)
        {
            if (surface == null)
                return transform.position;

            _previousProgress = _currentProgress;

            if (_isTransitioning)
            {
                AdvanceTransition(deltaTime);
                return GetCurrentWorldPosition(worldHeight, _currentProgress);
            }

            float edgeLimit = GetEdgeLimit();
            float nextHorizontal = _horizontal + horizontalSpeed * deltaTime;
            int direction = nextHorizontal >= edgeLimit && horizontalSpeed > 0f
                ? 1
                : nextHorizontal <= -edgeLimit && horizontalSpeed < 0f
                    ? -1
                    : 0;

            if (direction != 0)
            {
                BeginTransition(direction);
                return GetCurrentWorldPosition(worldHeight, 0f);
            }

            _horizontal = Mathf.Clamp(nextHorizontal, -edgeLimit, edgeLimit);
            return surface.SurfaceToWorld(
                _currentFace,
                _horizontal,
                worldHeight,
                _resolvedSurfaceOffset);
        }

        public Vector3 GetPositiveTangent()
        {
            if (surface == null)
                return transform.right;

            return _isTransitioning
                ? surface.GetTransitionPositiveTangent(
                    _fromFace,
                    _direction,
                    _currentProgress)
                : surface.GetFaceTangent(_currentFace);
        }

        public SurfaceTraversalSnapshot GetRenderSnapshot()
        {
            return new SurfaceTraversalSnapshot(
                surface,
                _currentFace,
                _fromFace,
                _toFace,
                _direction,
                GetRenderProgress(),
                _isTransitioning);
        }

        private void BeginTransition(int direction)
        {
            if (_isTransitioning)
                return;

            _direction = direction < 0 ? -1 : 1;
            _fromFace = _currentFace;
            _toFace = surface.GetAdjacentFace(_fromFace, _direction);
            _horizontal = _direction * GetEdgeLimit();
            _transitionTime = 0f;
            _previousProgress = 0f;
            _currentProgress = 0f;
            _isTransitioning = true;
        }

        private void AdvanceTransition(float deltaTime)
        {
            _transitionTime = Mathf.Min(transitionDuration, _transitionTime + deltaTime);
            float normalizedTime = Mathf.Clamp01(_transitionTime / transitionDuration);
            _currentProgress = EaseInOutQuadratic(normalizedTime);

            if (normalizedTime < 1f)
                return;

            _currentFace = _toFace;
            _horizontal = _direction > 0 ? -GetEdgeLimit() : GetEdgeLimit();
            _isTransitioning = false;
            _fromFace = _currentFace;
            _toFace = _currentFace;
            _direction = 0;
            _previousProgress = 0f;
            _currentProgress = 0f;
        }

        private Vector3 GetCurrentWorldPosition(float worldHeight, float progress)
        {
            if (!_isTransitioning)
            {
                return surface.SurfaceToWorld(
                    _currentFace,
                    _horizontal,
                    worldHeight,
                    _resolvedSurfaceOffset);
            }

            return surface.CornerArcToWorld(
                _fromFace,
                _direction,
                progress,
                worldHeight,
                _resolvedSurfaceOffset);
        }

        private float ResolveSurfaceOffset()
        {
            if (surface == null)
                return 0f;

            Collider bodyCollider = GetComponent<Collider>();
            if (bodyCollider == null)
                return surface.SurfaceOffset;

            Vector3 normal = surface.GetFaceNormal(_currentFace);
            Vector3 extents = bodyCollider.bounds.extents;
            float colliderExtent = Mathf.Abs(normal.x) * extents.x
                                   + Mathf.Abs(normal.y) * extents.y
                                   + Mathf.Abs(normal.z) * extents.z;
            return Mathf.Max(surface.SurfaceOffset, colliderExtent);
        }

        private float GetRenderProgress()
        {
            if (!_isTransitioning)
                return 0f;

            // Physics advances in fixed steps. The render frame usually lies between
            // two steps, so alpha=(renderTime-lastFixedTime)/fixedDeltaTime and
            // pRender=lerp(pPrevious,pCurrent,alpha) removes visible stepping.
            float interpolation = Time.fixedDeltaTime > Mathf.Epsilon
                ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime)
                : 1f;
            return Mathf.Lerp(_previousProgress, _currentProgress, interpolation);
        }

        private float GetEdgeLimit()
        {
            return Mathf.Max(0f, surface.HalfWidth - edgeInset);
        }

        private static float EaseInOutQuadratic(float value)
        {
            float t = Mathf.Clamp01(value);

            if (t < 0.5f)
                return 2f * t * t;

            float remaining = -2f * t + 2f;
            return 1f - remaining * remaining * 0.5f;
        }

        private static int NormalizeFace(int faceIndex)
        {
            int result = faceIndex % 4;
            return result < 0 ? result + 4 : result;
        }
    }
}
