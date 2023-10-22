using System.Linq;

namespace DotRecast.Recast
{
    /// <summary>
    /// 用于表示导航网格中的分区类型。分区类型定义了在构建导航网格时如何将多边形划分为连通区域。
    /// 这个类提供了三种分区类型：WATERSHED（分水岭）、MONOTONE（单调）和 LAYERS（图层）。
    ///
    ///  类提供了一种表示导航网格分区类型的方法，这在构建导航网格时非常有用，因为不同的分区类型可能会影响导航网格的性能和准确性。
    /// </summary>
    public class RcPartitionType
    {
        public static readonly RcPartitionType WATERSHED = new RcPartitionType(RcPartition.WATERSHED);
        public static readonly RcPartitionType MONOTONE = new RcPartitionType(RcPartition.MONOTONE);
        public static readonly RcPartitionType LAYERS = new RcPartitionType(RcPartition.LAYERS);

        // 包含了所有分区类型的实例。
        public static readonly RcPartitionType[] Values = { WATERSHED, MONOTONE, LAYERS };

        // 分别表示分区类型的枚举类型、整数值和名称。
        public readonly RcPartition EnumType;
        public readonly int Value;
        public readonly string Name;

        /// <summary>
        /// 这个构造函数接受一个 RcPartition 枚举类型参数，并根据该参数设置实例的 EnumType、Value 和 Name 字段。
        /// </summary>
        /// <param name="et"></param>
        private RcPartitionType(RcPartition et)
        {
            EnumType = et;
            Value = (int)et;
            Name = et.ToString();
        }

        /// <summary>
        /// 接受一个整数值参数，返回对应的 RcPartition 枚举类型。如果找不到对应的枚举类型，返回默认值 RcPartition.WATERSHED。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static RcPartition OfValue(int value)
        {
            return Values.FirstOrDefault(x => x.Value == value)?.EnumType ?? RcPartition.WATERSHED;
        }

        /// <summary>
        /// 接受一个 RcPartition 枚举类型参数，返回对应的 RcPartitionType 实例。如果找不到对应的实例，返回默认值 WATERSHED。
        /// </summary>
        /// <param name="partition"></param>
        /// <returns></returns>
        public static RcPartitionType Of(RcPartition partition)
        {
            return Values.FirstOrDefault(x => x.EnumType == partition) ?? WATERSHED;
        }
    }
}