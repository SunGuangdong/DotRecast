using DotRecast.Core;

namespace DotRecast.Recast.Geom
{
    /// <summary>
    /// 表示一个边界项（Bounds Item），它用于存储一个空间物体（如三角形）的边界信息。
    /// 这个类通常用于分块三角形网格（Chunky Triangle Mesh）的构建过程中，辅助计算物体的边界框和进行空间划分。
    /// </summary>
    public class BoundsItem
    {
        /// <summary>
        /// 表示物体的最小边界（x, y坐标）
        /// </summary>
        public RcVec2f bmin;
        /// <summary>
        /// 表示物体的最大边界（x, y坐标）
        /// </summary>
        public RcVec2f bmax;
        /// <summary>
        /// 表示物体在原始数据中的索引。
        /// </summary>
        public int i;
    }
}