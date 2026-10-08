using System.Collections.Generic;
using UnityEngine;

namespace DevLib.PolyNavMesh
{
    public static class Funnel
    {
        public static List<Vector2> StringPull(
            Vector2 start,
            Vector2 end,
            List<(Vector2 left, Vector2 right)> portals)
        {
            var path = new List<Vector2> { start };

            if (portals.Count == 0)
            {
                path.Add(end);
                return path;
            }

            // start와 end를 모두 degenerate 포털(양 끝점이 동일한 점)로 포함시킨다.
            // end를 메인 루프에 포함해야 목적지 근처에서 모서리를 연속으로 여러 번
            // 돌아야 하는 경우(다중 코너 endgame)가 올바르게 처리된다.
            // end를 루프 밖에서 한 번만 clamp하면 마지막 모서리 1개만 반영되어
            // 나머지를 직선으로 통과 → 벽 관통 경로가 만들어진다.
            //
            // 과거 버그(right를 end로 갱신한 직후 left 검사에서 spurious waypoint 생성)는
            // "end 자신은 절대 코너 waypoint로 추가하지 않는다"는 가드로 차단한다.
            // end는 항상 마지막에 단 한 번만 path에 추가된다.
            var pts = new List<(Vector2 left, Vector2 right)>(portals.Count + 2);
            pts.Add((start, start));
            pts.AddRange(portals);
            pts.Add((end, end));

            Vector2 apex       = start;
            Vector2 portalLeft = start;
            Vector2 portalRight = start;
            int apexIdx  = 0;
            int leftIdx  = 0;
            int rightIdx = 0;

            for (int i = 1; i < pts.Count; i++)
            {
                (Vector2 newLeft, Vector2 newRight) = pts[i];

                // ── 오른쪽 경계 갱신
                if (TriArea2(apex, portalRight, newRight) >= 0f)
                {
                    if (apex == portalRight || TriArea2(apex, portalLeft, newRight) < 0f)
                    {
                        portalRight = newRight;
                        rightIdx    = i;
                    }
                    else
                    {
                        // 왼쪽 모서리에 실이 걸림 → waypoint 확정 (end 자신은 제외)
                        if (portalLeft != apex && portalLeft != end)
                            path.Add(portalLeft);

                        apex       = portalLeft;
                        apexIdx    = leftIdx;
                        portalLeft = apex;
                        portalRight = apex;
                        leftIdx    = apexIdx;
                        rightIdx   = apexIdx;
                        i          = apexIdx;
                        continue;
                    }
                }

                // ── 왼쪽 경계 갱신
                if (TriArea2(apex, portalLeft, newLeft) <= 0f)
                {
                    if (apex == portalLeft || TriArea2(apex, portalRight, newLeft) > 0f)
                    {
                        portalLeft = newLeft;
                        leftIdx    = i;
                    }
                    else
                    {
                        // 오른쪽 모서리에 실이 걸림 → waypoint 확정 (end 자신은 제외)
                        if (portalRight != apex && portalRight != end)
                            path.Add(portalRight);

                        apex       = portalRight;
                        apexIdx    = rightIdx;
                        portalLeft = apex;
                        portalRight = apex;
                        leftIdx    = apexIdx;
                        rightIdx   = apexIdx;
                        i          = apexIdx;
                        continue;
                    }
                }
            }

            // 목적지는 항상 마지막에 한 번만 추가 (중복 방지)
            if (path.Count == 0 || path[^1] != end)
                path.Add(end);

            return path;
        }

        /// <summary>
        /// 삼각형 (a, b, c)의 부호 있는 넓이 × 2 (2D Cross Product).
        /// 양수: c가 벡터 a→b 의 왼쪽에 있음 (CCW)
        /// 음수: c가 벡터 a→b 의 오른쪽에 있음 (CW)
        /// 0   : a, b, c가 일직선
        /// </summary>
        private static float TriArea2(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 ba = b - a;
            Vector2 ca = c - a;
            return ba.x * ca.y - ba.y * ca.x;
        }
    }
}