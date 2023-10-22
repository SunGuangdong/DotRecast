using System.Collections.Generic;

namespace DotRecast.Recast
{
    /// <summary>
    /// 表示导航网格中的一个区域。
    /// 
    /// 通常用于在构建导航网格时存储和操作区域的信息。
    /// 这些信息有助于确定可行走区域、连接区域以及处理重叠和边界连接。
    /// 通过使用RcRegion类，可以更容易地处理导航网格中的不同区域，以便在寻路和导航过程中找到最佳路径。
    /// </summary>
    public class RcRegion
    {
        // 表示属于此区域的跨度数量。
        public int spanCount; // Number of spans belonging to this region
        // 表示区域的ID
        public int id; // ID of the region
        // 表示区域类型。
        public int areaType; // Are type.
        // 表示是否需要重新映射区域。
        public bool remap;
        // 表示区域是否已被访问。
        public bool visited;
        // 表示区域是否与其他区域重叠。
        public bool overlap;
        // 表示区域是否连接到边界。
        public bool connectsToBorder;
        // 表示区域的最小和最大高度。
        public int ymin, ymax;
        // 表示与该区域连接的其他区域的ID。
        public List<int> connections;
        // 表示与该区域重叠的其他区域的ID。
        public List<int> floors;

        public RcRegion(int i)
        {
            id = i;
            ymin = 0xFFFF;
            connections = new List<int>();
            floors = new List<int>();
        }
    }
}