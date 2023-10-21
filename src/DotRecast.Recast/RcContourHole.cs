namespace DotRecast.Recast
{
    /// <summary>
    /// 表示在场景空间中的一个轮廓空洞。在导航网格构建过程中，轮廓空洞用于表示连通区域内部的空洞。
    /// 这个类的主要目的是在导航网格构建过程中表示轮廓空洞，以便在后续步骤中正确处理连通区域内部的空洞。轮廓空洞通常用于生成导航网格的内部空洞多边形，从而生成最终的导航网格。
    /// </summary>
    public class RcContourHole
    {
        // 空洞中最左侧顶点的索引
        public int leftmost;
        // 空洞中最小x坐标
        public int minx;
        // 空洞中最小z坐标
        public int minz;
        // 与空洞关联的轮廓
        public RcContour contour;
    }
}