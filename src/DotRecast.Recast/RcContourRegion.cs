namespace DotRecast.Recast
{
    // 这个类的主要目的是在导航网格构建过程中表示轮廓区域，以便在后续步骤中正确处理连通区域及其内部的空洞。
    // 轮廓区域通常用于生成导航网格的边界多边形和内部空洞多边形，从而生成最终的导航网格。
    
    // 一个区域 肯定有一个大的外边缘，内部可能有0~n个空洞
    public class RcContourRegion
    {
        // 轮廓区域的外轮廓（RcContour对象）
        public RcContour outline;
        // 轮廓区域的内部空洞数组（RcContourHole[]）
        public RcContourHole[] holes;
        // 轮廓区域的内部空洞数量（整数）
        public int nholes;
    }
}