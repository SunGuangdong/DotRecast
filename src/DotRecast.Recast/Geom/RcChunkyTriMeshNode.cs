using DotRecast.Core;

namespace DotRecast.Recast.Geom
{
    /// <summary>
    /// 这个类表示分块三角形网格（Chunky Triangle Mesh）中的一个节点，每个节点代表一个空间区域。
    /// </summary>
    public class RcChunkyTriMeshNode
    {
        //节点所代表的空间区域的最小边界（x, y坐标） 
        public RcVec2f bmin;
        //节点所代表的空间区域的最大边界（x, y坐标）
        public RcVec2f bmax;
        // 节点在分块三角形网格中的索引。如果i为正数或0，则表示这是一个叶子节点，即该节点包含一些三角形；
        // 如果i为负数，则表示这是一个非叶子节点，它的子节点位于分块三角形网格中的 -i 位置。
        public int i;
        // 如果这个节点是一个叶子节点（包含三角形），则tris数组存储了这个节点所包含的三角形的顶点索引。
        // 每个三角形由3个顶点组成，因此tris数组的长度是3的倍数。如果这个节点不包含三角形（即是一个非叶子节点），则tris为null。
        public int[] tris;
    }
}