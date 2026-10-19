using UnityEngine;

namespace _Project._Scripts.Traversal
{
    [DisallowMultipleComponent]
    public sealed class TowerSurface : MonoBehaviour
    {
        private const int FaceCount = 4;

        [SerializeField, Min(0.01f)] private float halfWidth = 5f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.42f;

        public float HalfWidth => halfWidth;
        public float SurfaceOffset => surfaceOffset;
        public Vector3 Center => transform.position;

        public int GetAdjacentFace(int faceIndex, int direction)
        {
            int normalizedFace = NormalizeFace(faceIndex);
            int normalizedDirection = direction < 0 ? -1 : 1;
            return NormalizeFace(normalizedFace + normalizedDirection);
        }

        public Vector3 GetFaceNormal(int faceIndex)
        {
            Vector3 localNormal = NormalizeFace(faceIndex) switch
            {
                0 => Vector3.back,
                1 => Vector3.right,
                2 => Vector3.forward,
                _ => Vector3.left,
            };

            return transform.TransformDirection(localNormal).normalized;
        }

        public Vector3 GetFaceTangent(int faceIndex)
        {
            Vector3 localTangent = NormalizeFace(faceIndex) switch
            {
                0 => Vector3.right,
                1 => Vector3.forward,
                2 => Vector3.left,
                _ => Vector3.back,
            };

            return transform.TransformDirection(localTangent).normalized;
        }

        public Vector3 SurfaceToWorld(int faceIndex, float horizontal, float worldHeight)
        {
            return SurfaceToWorld(faceIndex, horizontal, worldHeight, surfaceOffset);
        }

        public Vector3 SurfaceToWorld(
            int faceIndex,
            float horizontal,
            float worldHeight,
            float agentOffset)
        {
            Vector3 centerAtHeight = new Vector3(Center.x, worldHeight, Center.z);

            // P = C + N(r + o) + Th
            // C: tower center, N: outward face normal, T: right-facing tangent,
            // r: tower half-width, o: agent offset, h: horizontal coordinate.
            // This converts the agent's simple "face + horizontal" coordinates to world space.
            return centerAtHeight
                   + GetFaceNormal(faceIndex) * (halfWidth + Mathf.Max(0f, agentOffset))
                   + GetFaceTangent(faceIndex) * horizontal;
        }

        public Vector3 CornerArcToWorld(
            int fromFace,
            int direction,
            float progress,
            float worldHeight)
        {
            return CornerArcToWorld(
                fromFace,
                direction,
                progress,
                worldHeight,
                surfaceOffset);
        }

        public Vector3 CornerArcToWorld(
            int fromFace,
            int direction,
            float progress,
            float worldHeight,
            float agentOffset)
        {
            // A corner is a quarter-circle, so its angle grows from 0 to PI/2 (90 degrees).
            // The same progress is also used by the camera, keeping both motions synchronized.
            float theta = Mathf.Clamp01(progress) * Mathf.PI * 0.5f;
            int normalizedDirection = direction < 0 ? -1 : 1;
            Vector3 normal = GetFaceNormal(fromFace);
            Vector3 tangent = GetFaceTangent(fromFace);

            Vector3 corner = new Vector3(Center.x, worldHeight, Center.z)
                             + normal * halfWidth
                             + tangent * (normalizedDirection * halfWidth);

            // O(theta) = N cos(theta) + direction * T sin(theta)
            // At theta=0 this is N; at theta=PI/2 it becomes direction*T.
            // Multiplying by the agent offset makes a small arc around the physical corner.
            Vector3 outward = normal * Mathf.Cos(theta)
                              + tangent * (normalizedDirection * Mathf.Sin(theta));
            return corner + outward * Mathf.Max(0f, agentOffset);
        }

        public Vector3 GetTransitionPositiveTangent(
            int fromFace,
            int direction,
            float progress)
        {
            float theta = Mathf.Clamp01(progress) * Mathf.PI * 0.5f;
            int normalizedDirection = direction < 0 ? -1 : 1;
            Vector3 normal = GetFaceNormal(fromFace);
            Vector3 tangent = GetFaceTangent(fromFace);

            // This is the quarter-circle's direction of travel expressed as the
            // positive tangent. It rotates T continuously into -direction*N.
            return (tangent * Mathf.Cos(theta)
                    - normal * (normalizedDirection * Mathf.Sin(theta))).normalized;
        }

        public float GetViewYaw(int faceIndex)
        {
            Vector3 inward = -GetFaceNormal(faceIndex);
            return Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
        }

        public float EvaluateViewYaw(SurfaceTraversalSnapshot snapshot)
        {
            int face = snapshot.IsTransitioning ? snapshot.FromFace : snapshot.CurrentFace;
            float fromYaw = GetViewYaw(face);
            if (!snapshot.IsTransitioning)
                return fromYaw;

            float toYaw = GetViewYaw(snapshot.ToFace);

            // yaw = fromYaw + shortestSignedAngle(fromYaw, toYaw) * progress
            // DeltaAngle is important at the 0/360 boundary: it chooses -90 instead
            // of +270, so every corner always takes the short quarter-turn.
            return fromYaw + Mathf.DeltaAngle(fromYaw, toYaw) * snapshot.Progress;
        }

        private static int NormalizeFace(int faceIndex)
        {
            int result = faceIndex % FaceCount;
            return result < 0 ? result + FaceCount : result;
        }
    }
}
