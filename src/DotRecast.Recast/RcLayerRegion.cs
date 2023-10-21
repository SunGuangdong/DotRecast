using System.Collections.Generic;

namespace DotRecast.Recast
{
    // 表示一个高度场层的区域。这个类通常用于在构建导航网格时存储地形高度信息的子集。
    // 这个类用于在导航网格构建过程中存储地形高度信息的子集。通过使用高度场区域，可以将地形划分为多个子集，从而更容易地进行导航和碰撞检测计算。
    // RcLayerRegion类可与RcHeightfieldLayer类和RcHeightfieldLayerSet类结合使用，以表示和操作高度场层的区域。
    public class RcLayerRegion
    {
        // 区域的唯一标识符。
        public int id;
        // 与该区域关联的高度场层的ID。
        public int layerId;
        // 一个布尔值，表示是否为基础区域。
        public bool @base;
        // 区域的最小y边界。区域的最大y边界。
        public int ymin, ymax;
        // 一个整数列表，表示与该区域关联的高度场层的ID。
        public List<int> layers;
        // 一个整数列表，表示与该区域相邻的区域的ID。
        public List<int> neis;

        
        public RcLayerRegion(int i)
        {
            id = i;  // 区域的唯一标识符。
            ymin = 0xFFFF;
            layerId = 0xff;
            layers = new List<int>();
            neis = new List<int>();
        }
    };
}