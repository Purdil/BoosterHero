using UnityEngine;

namespace _Project._Scripts.Traversal
{
    public readonly struct SurfaceTraversalSnapshot
    {
        public SurfaceTraversalSnapshot(
            TowerSurface surface,
            int currentFace,
            int fromFace,
            int toFace,
            int direction,
            float progress,
            bool isTransitioning)
        {
            Surface = surface;
            CurrentFace = currentFace;
            FromFace = fromFace;
            ToFace = toFace;
            Direction = direction;
            Progress = Mathf.Clamp01(progress);
            IsTransitioning = isTransitioning;
        }

        public TowerSurface Surface { get; }
        public int CurrentFace { get; }
        public int FromFace { get; }
        public int ToFace { get; }
        public int Direction { get; }
        public float Progress { get; }
        public bool IsTransitioning { get; }
    }
}
