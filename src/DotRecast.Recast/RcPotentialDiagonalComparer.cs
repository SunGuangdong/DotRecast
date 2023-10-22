using System.Collections.Generic;

namespace DotRecast.Recast
{
    //它实现了 IComparer<RcPotentialDiagonal> 接口。这个类的主要作用是比较两个 RcPotentialDiagonal 对象，以便在排序或其他需要比较的场景中使用。
    //RcPotentialDiagonal 类型可能表示一个潜在的对角线连接，例如在寻路过程中的一个对角线跳跃。
    //这个类提供了一个简单的比较器，用于比较两个 RcPotentialDiagonal 对象。这在处理潜在对角线连接时可能非常有用，例如在寻路算法中。
    public class RcPotentialDiagonalComparer : IComparer<RcPotentialDiagonal>
    {
        //表示这个类的一个唯一实例。这个实例可以在整个应用程序中共享，以避免重复创建实例。
        public static readonly RcPotentialDiagonalComparer Shared = new RcPotentialDiagonalComparer();

        private RcPotentialDiagonalComparer()
        {
        }

        public int Compare(RcPotentialDiagonal va, RcPotentialDiagonal vb)
        {
            RcPotentialDiagonal a = va;
            RcPotentialDiagonal b = vb;
            return a.dist.CompareTo(b.dist);
        }
    }
}