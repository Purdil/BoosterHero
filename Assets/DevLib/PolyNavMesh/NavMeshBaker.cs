using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DevLib.PolyNavMesh
{
    
    public class NavMeshBaker : MonoBehaviour
    {
        [SerializeField] private Tilemap groundMap;
        [SerializeField] private Tilemap obstacleMap;
        [SerializeField] private NavMeshBakeDataSO navMeshData;

        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color polygonColor  = new Color(0.2f, 0.8f, 0.2f, 0.4f);
        [SerializeField] private Color portalColor   = Color.yellow;
        [SerializeField] private Color centerColor   = Color.green;

        [ContextMenu("Bake NavMesh")]
        private void Bake()
        {
            Debug.Assert(groundMap   != null, "groundMap is not assigned");
            Debug.Assert(obstacleMap != null, "obstacleMap is not assigned");
            Debug.Assert(navMeshData != null, "navMeshData SO is not assigned");

            navMeshData.Clear();

            HashSet<Vector3Int> walkable = CollectWalkableCells();
            List<RectInt> rects    = MergeIntoRectangles(walkable);
            rects = SplitRectsAtWallBoundaries(rects, walkable);
            BuildPolygons(rects, walkable);
            navMeshData.BuildRuntimeMap();

            Debug.Log($"[PolyNavMesh] Baked {navMeshData.polygons.Count} polygons from {walkable.Count} cells");
            SaveAsset();
        }

        // Step 1: 이동 가능한 셀 수집 

        private HashSet<Vector3Int> CollectWalkableCells()
        {
            var walkable = new HashSet<Vector3Int>();
            groundMap.CompressBounds();
            BoundsInt bounds = groundMap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (groundMap.HasTile(cell) && !obstacleMap.HasTile(cell))
                    walkable.Add(cell);
            }
            return walkable;
        }

        // Step 2a: 벽 경계에서 사각형 분할
        // MergeIntoRectangles 결과에서 한 변이 부분적으로만 벽에 닿은 경우,
        // 벽↔비벽 전환점에서 사각형을 분할하여 ShrinkRect이 정확히 동작하도록 한다.

        private List<RectInt> SplitRectsAtWallBoundaries(List<RectInt> rects, HashSet<Vector3Int> walkable)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                var next = new List<RectInt>();
                foreach (RectInt rect in rects)
                {
                    List<RectInt> split = TrySplitRect(rect, walkable);
                    if (split != null)
                    {
                        next.AddRange(split);
                        changed = true;
                    }
                    else
                    {
                        next.Add(rect);
                    }
                }
                rects = next;
            }
            return rects;
        }

        /// <summary>
        /// 사각형의 4변을 검사하여 벽↔비벽 전환이 있으면 그 지점에서 분할한다.
        /// 수평 변(상/하)의 전환 → x축 분할, 수직 변(좌/우)의 전환 → y축 분할.
        /// 첫 번째 발견한 전환점에서만 분할하고 반환한다 (나머지는 다음 반복에서 처리).
        /// </summary>
        private List<RectInt> TrySplitRect(RectInt rect, HashSet<Vector3Int> walkable)
        {
            // 상/하 변: x축 분할
            int? splitX = FindHorizontalSideSplit(rect, -1, walkable)
                   ?? FindHorizontalSideSplit(rect, +1, walkable);
            if (splitX.HasValue)
            {
                return new List<RectInt>
                {
                    new RectInt(rect.xMin, rect.yMin, splitX.Value - rect.xMin, rect.height),
                    new RectInt(splitX.Value, rect.yMin, rect.xMax - splitX.Value, rect.height)
                };
            }

            // 좌/우 변: y축 분할
            int? splitY = FindVerticalSideSplit(rect, -1, walkable)
                   ?? FindVerticalSideSplit(rect, +1, walkable);
            if (splitY.HasValue)
            {
                return new List<RectInt>
                {
                    new RectInt(rect.xMin, rect.yMin, rect.width, splitY.Value - rect.yMin),
                    new RectInt(rect.xMin, splitY.Value, rect.width, rect.yMax - splitY.Value)
                };
            }

            return null;
        }

        /// <summary>
        /// 수평 변(하: dy=-1, 상: dy=+1)을 x방향으로 스캔하여
        /// 벽이 끊어지는 전환이 발생하는 x좌표를 반환한다.
        /// </summary>
        private static int? FindHorizontalSideSplit(RectInt rect, int dy, HashSet<Vector3Int> walkable)
        {
            if (rect.width <= 1) return null;
            int checkY = dy < 0 ? rect.yMin - 1 : rect.yMax; //사각형의 y축 아래 또는 y축 위를 저장.

            bool prevWall = !walkable.Contains(new Vector3Int(rect.xMin, checkY, 0));
            for (int x = rect.xMin + 1; x < rect.xMax; x++)
            {
                bool wall = !walkable.Contains(new Vector3Int(x, checkY, 0));
                if (wall != prevWall) return x;
                prevWall = wall;
            }
            return null;
        }

        /// <summary>
        /// 수직 변(좌: dx=-1, 우: dx=+1)을 y방향으로 스캔하여
        /// 벽이 끊어지는 전환이 발생하는 y좌표를 반환한다.
        /// </summary>
        private static int? FindVerticalSideSplit(RectInt rect, int dx, HashSet<Vector3Int> walkable)
        {
            if (rect.height <= 1) return null;
            int checkX = dx < 0 ? rect.xMin - 1 : rect.xMax;

            bool prevWall = !walkable.Contains(new Vector3Int(checkX, rect.yMin, 0));
            for (int y = rect.yMin + 1; y < rect.yMax; y++)
            {
                bool wall = !walkable.Contains(new Vector3Int(checkX, y, 0));
                if (wall != prevWall) return y;
                prevWall = wall;
            }
            return null;
        }

        // Step 2b: Scan-line segment matching — 벽 위치에만 경계를 생성하는 최소 사각형 분해.
        // 각 행의 연속된 walkable 구간(segment)을 구하고, 바로 위 행에서 완전히 동일한
        // segment가 이어지면 수직으로 병합한다.
        // 결과: 모든 polygon 경계(portal 끝점)가 실제 벽 위치에만 존재한다.

        private List<RectInt> MergeIntoRectangles(HashSet<Vector3Int> walkable)
        {
            if (walkable.Count == 0) return new List<RectInt>();

            //워커블 셀들을 순회하면서 각 셀의 x,y축 하한 상한을 알아낸다.
            int yMin = int.MaxValue, yMax = int.MinValue;
            int xMin = int.MaxValue, xMax = int.MinValue;
            foreach (Vector3Int cell in walkable)
            {
                if (cell.y < yMin) yMin = cell.y;
                if (cell.y > yMax) yMax = cell.y;
                if (cell.x < xMin) xMin = cell.x;
                if (cell.x > xMax) xMax = cell.x;
            }

            List<RectInt> rects = new List<RectInt>();
            // 현재 확장 중인 사각형: key=(xMin, xMax exclusive), value=시작 y
            Dictionary<(int, int), int> active = new Dictionary<(int, int), int>();

            for (int y = yMin; y <= yMax + 1; y++) //마지막으로 돌던 사각형도 rect집합에 들어가게 하기 위해 +1까지 돌린다.
            {
                // 현재 행의 연속된 walkable 구간을 구한다 (가로로 쭉 돌면서 블럭들을 구함.)
                var currentSegs = new HashSet<(int start, int end)>();
                if (y <= yMax)
                {
                    int? segStart = null;
                    for (int x = xMin; x <= xMax + 1; x++)
                    {
                        bool isWalkable = x <= xMax && walkable.Contains(new Vector3Int(x, y, 0));
                        if (isWalkable && segStart == null)       segStart = x; //걸을 수 있는 셀이고 시작이라면 x를 Segment 시작으로
                        else if (!isWalkable && segStart != null) { currentSegs.Add((segStart.Value, x)); segStart = null; }
                        //걸을 수 없고, 시작한 상태라면 현재 세그먼트를 시작과 끝으로 해서 currentSegs에 저장. 
                    }
                }

                // 이전 행과 완전히 일치하는 segment는 계속 확장, 아니면 사각형 확정
                var newActive = new Dictionary<(int start, int end), int>();
                foreach ((int start, int end) seg in currentSegs)
                {
                    //완전 일치하는 사각형이 있다면 해당 사각형의 y값을 가져오고 그렇지 않다면 현재 y값을 넣는다.
                    newActive[seg] = active.GetValueOrDefault(seg, y);
                }
                
                //active중에서 현재 currentSeg에 속하지 못한 것들은 이번행에서 끊긴 사각형들이다. 따라서 사각형으로 정리.
                foreach (KeyValuePair<(int start, int end), int> kv in active)
                {
                    if (!currentSegs.Contains(kv.Key))
                        rects.Add(new RectInt(kv.Key.Item1, kv.Value, kv.Key.Item2 - kv.Key.Item1, y - kv.Value));
                }
                active = newActive; //새로운 활성 사각형으로 갱신.
            }
            return rects;
        }

        //Step 3: 폴리곤 생성 및 Portal 연결

        /// <summary>
        /// 각 사각형에서 '장애물(벽)과 맞닿은 변'만 agentRadius만큼 안쪽으로 축소한 뒤
        /// 폴리곤과 포털을 만든다.
        ///
        /// - 이웃 폴리곤과 공유하는 변(=포털이 생기는 변)은 절대 축소하지 않으므로 연결성이 깨지지 않는다.
        /// - 포털 끝점은 '축소된' 사각형 기준으로 다시 계산되어, 벽 모서리에서 자연스럽게
        ///   agentRadius만큼 떨어진다 → 에이전트가 벽에 걸리지 않는다.
        /// - 좁은 통로에서 폴리곤이 뒤집히거나 사라지지 않도록 축소량을 clamp한다.
        ///
        /// 주의: 벽 이격이 여기(베이크)에서 끝나므로 Funnel은 포털 끝점을 그대로 잇는
        ///       순수 string-pull만 수행한다 (NavAgent2D 참고). 런타임에서 추가 이격하지 않는다.
        /// </summary>
        private void BuildPolygons(List<RectInt> rects, HashSet<Vector3Int> walkable)
        {
            float radius = navMeshData.AgentData != null ? navMeshData.AgentData.AgentRadius : 0f;

            // 벽과 맞닿은 변을 축소한 월드 좌표 사각형 (인덱스는 rects와 1:1 대응)
            var shrunk = new WorldRect[rects.Count];
            for (int i = 0; i < rects.Count; i++)
                shrunk[i] = ShrinkRect(rects[i], walkable, radius);

            // 축소된 직사각형 → PolygonData
            for (int i = 0; i < rects.Count; i++)
                navMeshData.polygons.Add(RectToPolygon(i, shrunk[i]));

            // 인접 쌍 검사 → Portal 양방향 연결
            // (인접 여부는 원본 셀 좌표로 판별하고, 끝점 좌표는 축소본을 사용한다)
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                    TryAddPortal(rects[i], rects[j], shrunk[i], shrunk[j],
                                 navMeshData.polygons[i], navMeshData.polygons[j]);
        }

        /// <summary>축소 후의 사각형 (월드 좌표, 축이 정렬된 AABB).</summary>
        private struct WorldRect { public float xMin, yMin, xMax, yMax; }

        /// <summary>
        /// 사각형의 4개 변 중 '완전히 벽과 맞닿은 변'만 radius만큼 안쪽으로 민다.
        /// 한 칸이라도 walkable 이웃이 있는 변(=포털이 생기는 변)은 그대로 둔다.
        /// </summary>
        private WorldRect ShrinkRect(RectInt rect, HashSet<Vector3Int> walkable, float radius)
        {
            Vector2 bl = CellCornerToWorld(groundMap, rect.xMin, rect.yMin);
            Vector2 tr = CellCornerToWorld(groundMap, rect.xMax, rect.yMax);

            float left   = IsSideWall(rect, -1, 0, walkable) ? radius : 0f; // 왼쪽 변
            float right  = IsSideWall(rect, +1, 0, walkable) ? radius : 0f; // 오른쪽 변
            float bottom = IsSideWall(rect, 0, -1, walkable) ? radius : 0f; // 아래 변
            float top    = IsSideWall(rect, 0, +1, walkable) ? radius : 0f; // 위 변

            // 마주보는 두 변의 축소량 합이 폭/높이를 넘으면 폴리곤이 뒤집힌다 → clamp
            ClampInset(ref left,   ref right, tr.x - bl.x);
            ClampInset(ref bottom, ref top,   tr.y - bl.y);

            return new WorldRect
            {
                xMin = bl.x + left,
                yMin = bl.y + bottom,
                xMax = tr.x - right,
                yMax = tr.y - top
            };
        }

        // 좁은 통로 보호: 마주보는 변의 축소량 합(a+b)이 extent를 넘지 않게 비례 축소한다.
        // 최소 두께 MinExtent는 남겨 폴리곤/포털이 완전히 사라지지 않도록 한다.
        private const float MinExtent = 0.05f;
        private static void ClampInset(ref float a, ref float b, float extent)
        {
            float max = extent - MinExtent;
            if (a + b > max && a + b > 0f)
            {
                float scale = max / (a + b);
                a *= scale;
                b *= scale;
            }
        }

        // (dx, dy) 방향 바깥쪽 셀들이 '전부' walkable이 아니면 true (= 그 변 전체가 벽).
        // dx != 0: 좌/우 변을 y축을 따라 스캔 / dy != 0: 상/하 변을 x축을 따라 스캔.
        private static bool IsSideWall(RectInt rect, int dx, int dy, HashSet<Vector3Int> walkable)
        {
            if (dx != 0)
            {
                int x = dx < 0 ? rect.xMin - 1 : rect.xMax; // xMax는 exclusive → 바깥 첫 칸
                for (int y = rect.yMin; y < rect.yMax; y++)
                    if (walkable.Contains(new Vector3Int(x, y, 0))) return false;
            }
            else
            {
                int y = dy < 0 ? rect.yMin - 1 : rect.yMax;
                for (int x = rect.xMin; x < rect.xMax; x++)
                    if (walkable.Contains(new Vector3Int(x, y, 0))) return false;
            }
            return true;
        }

        private static PolygonData RectToPolygon(int id, WorldRect worldRect)
        {
            // 꼭짓점을 CCW(반시계 방향)로 정렬 — ContainsPoint 판별에 필요
            Vector2 bl = new Vector2(worldRect.xMin, worldRect.yMin);
            Vector2 br = new Vector2(worldRect.xMax, worldRect.yMin);
            Vector2 tr = new Vector2(worldRect.xMax, worldRect.yMax);
            Vector2 tl = new Vector2(worldRect.xMin, worldRect.yMax);

            return new PolygonData
            {
                id       = id,
                center   = (bl + br + tr + tl) * 0.25f,
                vertices = new[] { bl, br, tr, tl },  // CCW
                portals  = new List<PortalData>()
            };
        }

        /// <summary>
        /// 두 직사각형이 엣지를 공유하면 Portal을 양방향으로 추가한다.
        /// 공유 변은 벽이 아니므로 축소되지 않아 양쪽 좌표가 정확히 일치한다.
        /// 포털 끝점(수직 방향 범위)은 축소된 사각형 기준으로 계산되어 벽에서 떨어진다.
        /// </summary>
        private static void TryAddPortal(RectInt rectA, RectInt rectB, WorldRect wRectA, WorldRect wRectB,
            PolygonData polyA, PolygonData polyB)
        {
            const float eps = 1e-4f;

            // 수평 인접: 공유 세로 엣지  → 포털은 Y축 방향 세그먼트
            if (rectA.xMax == rectB.xMin || rectB.xMax == rectA.xMin)
            {
                // 공유 변(= 한쪽의 오른쪽 변)은 축소되지 않았으므로 그 x를 그대로 쓴다
                float   px   = (rectA.xMax == rectB.xMin) ? wRectA.xMax : wRectB.xMax;
                float   yMin = Mathf.Max(wRectA.yMin, wRectB.yMin); //2개의 min중 큰 것
                float   yMax = Mathf.Min(wRectA.yMax, wRectB.yMax); //2개의 max중 작은것 으로 교집을 찾는다.
                if (yMax - yMin <= eps) return;

                Vector2 pA = new Vector2(px, yMin);
                Vector2 pB = new Vector2(px, yMax);
                polyA.portals.Add(new PortalData { pointA = pA, pointB = pB, neighborId = polyB.id });
                polyB.portals.Add(new PortalData { pointA = pA, pointB = pB, neighborId = polyA.id });
            }
            // 수직 인접: 공유 가로 엣지  → 포털은 X축 방향 세그먼트
            else if (rectA.yMax == rectB.yMin || rectB.yMax == rectA.yMin)
            {
                float   py   = (rectA.yMax == rectB.yMin) ? wRectA.yMax : wRectB.yMax;
                float   xMin = Mathf.Max(wRectA.xMin, wRectB.xMin);
                float   xMax = Mathf.Min(wRectA.xMax, wRectB.xMax);
                if (xMax - xMin <= eps) return;

                Vector2 pA = new Vector2(xMin, py);
                Vector2 pB = new Vector2(xMax, py);
                polyA.portals.Add(new PortalData { pointA = pA, pointB = pB, neighborId = polyB.id });
                polyB.portals.Add(new PortalData { pointA = pA, pointB = pB, neighborId = polyA.id });
            }
        }

        /// <summary>
        /// 셀 격자 좌표 (정수 모서리)를 월드 좌표로 변환한다.
        /// CellToWorld는 셀 중심이 아닌 셀 경계를 기준으로 한다.
        /// </summary>
        private static Vector2 CellCornerToWorld(Tilemap groundMap, int x, int y)
            => groundMap.CellToWorld(new Vector3Int(x, y, 0));

        private void SaveAsset()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(navMeshData);
            AssetDatabase.SaveAssets();
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || navMeshData == null) return;

            foreach (var poly in navMeshData.polygons)
            {
                // 폴리곤 윤곽선
                Gizmos.color = polygonColor;
                Handles.color = polygonColor;
                DrawPolygonGizmo(poly.vertices);

                // 중심점
                Gizmos.color = centerColor;
                Gizmos.DrawWireSphere(poly.center, 0.1f);

                // Portal 엣지
                Gizmos.color = portalColor;
                foreach (var portal in poly.portals)
                {
                    Gizmos.DrawLine(portal.pointA, portal.pointB);
                    Gizmos.DrawWireSphere((portal.pointA + portal.pointB) * 0.5f, 0.2f);
                }
            }
        }

        private static void DrawPolygonGizmo(Vector2[] verts)
        {
            for (int i = 0; i < verts.Length; i++)
            {
                Handles.DrawLine(verts[i], verts[(i + 1) % verts.Length], 4f);
            }
        }
#endif
    }
}