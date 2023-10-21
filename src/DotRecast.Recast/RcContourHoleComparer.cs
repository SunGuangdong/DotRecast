using System.Collections.Generic;

namespace DotRecast.Recast
{
    // 用于比较两个RcContourHole对象。
    // 这个类的主要目的是在导航网格构建过程中对轮廓空洞进行排序，以便在后续步骤中正确处理连通区域内部的空洞。
    // 通过实现IComparer<RcContourHole>接口，可以方便地将RcContourHoleComparer用于诸如Array.Sort()或List<T>.Sort()之类的排序方法。
    public class RcContourHoleComparer : IComparer<RcContourHole>
    {
        // 一个静态只读的RcContourHoleComparer实例，可在多个地方共享
        public static readonly RcContourHoleComparer Shared = new RcContourHoleComparer();

        private RcContourHoleComparer()
        {
        }

        // 比较两个RcContourHole对象。首先比较它们的minx值，如果相等，则比较它们的minz值。返回比较结果（整数）
        public int Compare(RcContourHole a, RcContourHole b)
        {
            if (a.minx == b.minx)
            {
                return a.minz.CompareTo(b.minz);
            }
            else
            {
                return a.minx.CompareTo(b.minx);
            }
        }
    }
}