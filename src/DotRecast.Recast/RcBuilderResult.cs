using DotRecast.Core;

namespace DotRecast.Recast
{
    /// <summary>
    /// 是一个用于存储导航网格构建结果的类
    /// </summary>
    public class RcBuilderResult
    {
        // 瓦片在网格中的坐标（X和Z轴）。
        public readonly int tileX;
        public readonly int tileZ;
        
        // 紧凑高度场（RcCompactHeightfield）。
        private readonly RcCompactHeightfield chf;
        // 轮廓集（RcContourSet）
        private readonly RcContourSet cs;
        // 多边形网格（RcPolyMesh）
        private readonly RcPolyMesh pmesh;
        // 详细多边形网格（RcPolyMeshDetail）
        private readonly RcPolyMeshDetail dmesh;
        // 实心高度场（RcHeightfield）。
        private readonly RcHeightfield solid;
        // 用于收集构建过程中的性能数据和日志信息的RcTelemetry对象
        private readonly RcTelemetry telemetry;

        // 接受瓦片坐标、实心高度场、紧凑高度场、轮廓集、多边形网格、详细多边形网格和上下文作为参数。
        public RcBuilderResult(int tileX, int tileZ, RcHeightfield solid, RcCompactHeightfield chf, RcContourSet cs, RcPolyMesh pmesh, RcPolyMeshDetail dmesh, RcTelemetry ctx)
        {
            this.tileX = tileX;
            this.tileZ = tileZ;
            this.solid = solid;
            this.chf = chf;
            this.cs = cs;
            this.pmesh = pmesh;
            this.dmesh = dmesh;
            telemetry = ctx;
        }

        /// <summary>
        /// 返回多边形网格（RcPolyMesh）
        /// </summary>
        /// <returns></returns>
        public RcPolyMesh GetMesh()
        {
            return pmesh;
        }

        /// <summary>
        /// 返回详细多边形网格（RcPolyMeshDetail）。
        /// </summary>
        /// <returns></returns>
        public RcPolyMeshDetail GetMeshDetail()
        {
            return dmesh;
        }

        /// <summary>
        /// 返回紧凑高度场（RcCompactHeightfield）
        /// </summary>
        /// <returns></returns>
        public RcCompactHeightfield GetCompactHeightfield()
        {
            return chf;
        }

        /// <summary>
        /// 返回轮廓集（RcContourSet）
        /// </summary>
        /// <returns></returns>
        public RcContourSet GetContourSet()
        {
            return cs;
        }

        /// <summary>
        /// 返回实心高度场（RcHeightfield）
        /// </summary>
        /// <returns></returns>
        public RcHeightfield GetSolidHeightfield()
        {
            return solid;
        }

        /// <summary>
        /// 返回用于收集构建过程中的性能数据和日志信息的RcTelemetry对象。
        /// </summary>
        /// <returns></returns>
        public RcTelemetry GetTelemetry()
        {
            return telemetry;
        }
    }
}