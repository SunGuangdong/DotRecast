namespace DotRecast.Recast
{
    /// 用于表示轮廓构建的标志。这些标志通常在调用rcBuildContours方法时使用。
    /// 在实际应用中，可以根据需要选择使用这些标志来控制轮廓构建的细节。例如，如果要生成一个更精确的边缘表示，可以使用这些标志来对边缘进行镶嵌处理。
    /// Contour build flags.
    /// @see rcBuildContours
    public static class RcBuildContoursFlags
    {
        /// <summary>
        /// 表示在轮廓简化过程中，对实心（不可通过）的边缘进行镶嵌处理。这有助于更好地表示障碍物或墙壁的边缘。
        /// </summary>
        public const int RC_CONTOUR_TESS_WALL_EDGES = 0x01; //< Tessellate solid (impassable) edges during contour simplification.
        /// <summary>
        /// 表示在轮廓简化过程中，对区域之间的边缘进行镶嵌处理。这有助于更好地表示不同区域之间的边界。
        /// </summary>
        public const int RC_CONTOUR_TESS_AREA_EDGES = 0x02; //< Tessellate edges between areas during contour simplification.
    }
}