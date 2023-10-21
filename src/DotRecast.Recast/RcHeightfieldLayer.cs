using DotRecast.Core;

namespace DotRecast.Recast
{
    /// Represents a heightfield layer within a layer set.
    /// @see rcHeightfieldLayerSet
    /// 表示一个高度场层集中的高度场层。这个类通常用于在构建导航网格时存储地形高度信息的子集。类中包含以下属性：
    /// 这个类用于在导航网格构建过程中存储地形高度信息的子集。通过使用高度场层，可以将地形划分为多个子集，从而更容易地进行导航和碰撞检测计算。
    public class RcHeightfieldLayer
    {
        // 世界空间中的最小边界坐标（一个RcVec3f对象，表示(x, y, z)）。
        public RcVec3f bmin = new RcVec3f();

        // 世界空间中的最大边界坐标（一个RcVec3f对象，表示(x, y, z)）。
        /// < The minimum bounds in world space. [(x, y, z)]
        public RcVec3f bmax = new RcVec3f();

        //每个单元格在xz平面上的大小。
        /// < The maximum bounds in world space. [(x, y, z)]
        public float cs;

        // 每个单元格在y轴上的高度（最小增量）。
        /// < The size of each cell. (On the xz-plane.)
        public float ch;

        // 高度场的宽度（沿x轴的单元格单位）。
        /// < The height of each cell. (The minimum increment along the y-axis.)
        public int width;

        // 高度场的高度（沿z轴的单元格单位）。
        /// < The width of the heightfield. (Along the x-axis in cell units.)
        public int height;

        // 可用数据的最小x边界。
        /// < The height of the heightfield. (Along the z-axis in cell units.)
        public int minx;

        // 可用数据的最大x边界。
        /// < The minimum x-bounds of usable data.
        public int maxx;

        // 可用数据的最小y边界（沿z轴）。
        /// < The maximum x-bounds of usable data.
        public int miny;

        // 可用数据的最大y边界（沿z轴）。
        /// < The minimum y-bounds of usable data. (Along the z-axis.)
        public int maxy;

        // 可用数据的最小高度边界（沿y轴）。
        /// < The maximum y-bounds of usable data. (Along the z-axis.)
        public int hmin;

        // 可用数据的最大高度边界（沿y轴）。
        /// < The minimum height bounds of usable data. (Along the y-axis.)
        public int hmax;

        // 高度场数组（大小：width * height）。
        /// < The maximum height bounds of usable data. (Along the y-axis.)
        public int[] heights;

        // 区域ID数组（大小：与heights相同）。
        /// < The heightfield. [Size: width * height]
        public int[] areas;

        // 打包的邻居连接信息数组（大小：与heights相同）。
        /// < Area ids. [Size: Same as #heights]
        public int[] cons; 
        /// < Packed neighbor connection information. [Size: Same as #heights]
    }
}