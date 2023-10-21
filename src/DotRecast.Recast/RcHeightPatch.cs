namespace DotRecast.Recast
{
    // 表示一个高度场的局部区域。这个类通常用于在构建导航网格时存储地形高度信息的子集。
    // 这个类用于在导航网格构建过程中存储地形高度信息的子集。通过使用高度补丁，可以将地形划分为多个子集，从而更容易地进行导航和碰撞检测计算。
    // RcHeightPatch类可与RcHeightfield类和RcHeightfieldLayer类结合使用，以表示和操作高度场的局部区域。
    public class RcHeightPatch
    {
        // 高度补丁在高度场中的最小x坐标。
        public int xmin;
        // 高度补丁在高度场中的最小y坐标。
        public int ymin;
        // 高度补丁的宽度（沿x轴的单元格单位）。
        public int width;
        // 高度补丁的高度（沿y轴的单元格单位）。
        public int height;
        // 一个整数数组，表示高度补丁的高度数据。
        public int[] data;
    }
}