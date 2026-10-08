namespace DevLib.PolyNavMesh
{
    // ── A* 탐색용 내부 노드
    // NavPolygon(정적 데이터)을 수정하지 않고 탐색 상태를 별도로 관리한다.
    
    public sealed class AStarNode
    {
        public readonly NavPolygon polygon;
        public float G;
        public float F;
        public AStarNode parent;

        public AStarNode(NavPolygon polygon) => this.polygon = polygon;
    }
}