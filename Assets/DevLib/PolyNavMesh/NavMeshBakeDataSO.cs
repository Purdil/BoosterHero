using System.Collections.Generic;
using UnityEngine;

namespace DevLib.PolyNavMesh
{
    [CreateAssetMenu(fileName = "Baked data", menuName = "Lib/PolyNavMesh/Baked data", order = 15)]
    public class NavMeshBakeDataSO : ScriptableObject
    {
        [field: SerializeField] public NavAgentDataSO AgentData { get; private set; }
        public List<PolygonData> polygons = new List<PolygonData>();

        // 런타임 전용 — ID → NavPolygon 빠른 조회
        private Dictionary<int, NavPolygon> _runtimeMap;

        private void OnEnable() => BuildRuntimeMap();

        /// <summary>
        /// 직렬화 데이터로부터 런타임 딕셔너리를 빌드한다.
        /// NavMeshBaker가 베이킹을 완료한 뒤에도 호출된다.
        /// </summary>
        public void BuildRuntimeMap()
        {
            _runtimeMap = new Dictionary<int, NavPolygon>(polygons.Count);
            
            foreach (var data in polygons)
            {
                _runtimeMap[data.id] = new NavPolygon
                {
                    id       = data.id,
                    center   = data.center,
                    vertices = data.vertices,
                    portals  = data.portals
                };
            }
            Debug.Log($"[NavMesh] Building runtime map with {polygons.Count} polygons");
        }

        /// <summary>
        /// 넘겨진 월드 좌표를 포함하는 폴리곤을 반환한다. O(n). 선형탐색임.
        /// </summary>
        public bool GetPolygonAt(Vector2 worldPoint, out NavPolygon polygon)
        {
            foreach (var p in _runtimeMap.Values)
            {
                if (ContainsPoint(p.vertices, worldPoint))
                {
                    polygon = p;
                    return true;
                }
            }
            polygon = null;
            return false;
        }

        public bool TryGetPolygon(int id, out NavPolygon polygon)
        {
            polygon = null;
            return _runtimeMap != null && _runtimeMap.TryGetValue(id, out polygon);
        }

        /// <summary>
        /// 월드 좌표와 가장 가까운 폴리곤(중심 기준)을 반환한다.
        /// 경로가 없는 경우 가장 가까운 곳까지의 이동을 만들기 위해서 (단 중심점 기준이므로 사소한 오차 가능) 
        /// </summary>
        public bool GetNearestPolygon(Vector2 worldPoint, out NavPolygon polygon)
        {
            polygon = null;
            if (_runtimeMap == null || _runtimeMap.Count == 0) return false;

            float bestSqr = float.MaxValue;
            foreach (var p in _runtimeMap.Values)
            {
                float sqr = (p.center - worldPoint).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; polygon = p; }
            }
            return polygon != null;
        }
            

        public void Clear() => polygons.Clear();

        // ── 볼록 폴리곤 내부 판별 (Cross Product 방식) ───────────────────────────
        // CCW 순서로 정렬된 볼록 폴리곤에서, 모든 엣지에 대해 점이 왼쪽에 있으면 내부.
        // 첫 정점은 마지막 정점으로 향하는 라인, 두번째부터는 이전 정점으로 향하도록 라인을 잡고(라인 a)
        // 이 라인과 지정된 점과 이번 정점을 잇는 라인(라인 b)을 외적한다. 이때 점이 사각형의 안쪽에 존재한다면 
        // 반드시 첫번째 라인이 반드시 두번째 라인보다 바깥에 위치하게 된어. 외적결과가 + 가 나온다.
        private static bool ContainsPoint(Vector2[] verts, Vector2 p)
        {
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                Vector2 a = verts[j], b = verts[i];
                // Cross(b-a, p-a): 음수면 p가 엣지의 오른쪽(외부) (z값만 계산해서 최적화.)
                float cross = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                if (cross < 0f) return false;
            }
            return true;
        }
    }
}