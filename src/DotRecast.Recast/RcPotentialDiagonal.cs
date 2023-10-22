namespace DotRecast.Recast
{
    /// <summary>
    /// 表示潜在的对角线连接。这个类可以用于寻路算法中，例如在处理网格上的对角线跳跃时。
    /// </summary>
    public class RcPotentialDiagonal
    {
        /// <summary>
        /// 表示对角线连接的距离。在寻路过程中，这个距离可以用于比较和排序潜在的对角线连接，以找到最佳的路径。
        /// </summary>
        public int dist;
        /// <summary>
        /// 表示对角线连接的顶点索引。这个索引可以用于在网格中查找对应的顶点位置，从而获取对角线连接的具体坐标。
        /// </summary>
        public int vert;
    }
}