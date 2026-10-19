using _Project._Scripts.Traversal;
using Unity.Cinemachine;
using UnityEngine;

namespace _Project._Scripts.Camera
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class FacingCam : MonoBehaviour
    {
        [SerializeField] private SurfaceAgent target;
        [SerializeField, Min(0.01f)] private float cameraDistance = 10f;
        [SerializeField, Min(0f)] private float verticalSmoothTime = 0.2f;
        [SerializeField] private float verticalLookAhead;

        private CinemachineCamera _cinemachineCamera;
        private float _focusHeight;
        private float _verticalVelocity;
        private bool _heightInitialized;

        private void Awake()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            ResolveTarget();
        }

        private void OnEnable()
        {
            _heightInitialized = false;
        }

        private void LateUpdate()
        {
            if (target == null)
                ResolveTarget();

            if (target == null || !target.IsBound)
                return;

            SurfaceTraversalSnapshot snapshot = target.GetRenderSnapshot();
            TowerSurface surface = snapshot.Surface;
            float targetHeight = target.transform.position.y + verticalLookAhead;
            if (!_heightInitialized)
            {
                _focusHeight = targetHeight;
                _heightInitialized = true;
            }
            else
            {
                _focusHeight = Mathf.SmoothDamp(
                    _focusHeight,
                    targetHeight,
                    ref _verticalVelocity,
                    verticalSmoothTime);
            }

            float yaw = surface.EvaluateViewYaw(snapshot);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 focus = new Vector3(surface.Center.x, _focusHeight, surface.Center.z);
            Vector3 position = focus - rotation * Vector3.forward * cameraDistance;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void ResolveTarget()
        {
            if (target != null || _cinemachineCamera == null)
                return;

            Transform trackingTarget = _cinemachineCamera.Target.TrackingTarget;
            if (trackingTarget != null)
                target = trackingTarget.GetComponent<SurfaceAgent>();
        }
    }
}
