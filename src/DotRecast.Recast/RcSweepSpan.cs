namespace DotRecast.Recast
{
    // 用于在高度场层构建过程中表示扫描跨度
    // 通常用于RcLayers.BuildHeightfieldLayers方法中，以便在构建高度场层时跟踪扫描跨度的信息。
    // 这些信息有助于确定区域之间的连接以及如何合并相邻区域以创建2D层。
    public class RcSweepSpan
    {
        // 表示行ID
        public int rid; // row id
        // 表示区域ID。
        public int id; // region id
        // 表示样本数量
        public int ns; // number samples
        // 表示邻居ID
        public int nei; // neighbour id
    }
}