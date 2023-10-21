/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
recast4j copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
DotRecast Copyright (c) 2023 Choi Ikpil ikpil@naver.com

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:
1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software. If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using DotRecast.Core;

namespace DotRecast.Recast
{
    /// <summary>
    /// 是一个用于存储导航网格构建配置的类。
    /// </summary>
    public class RcBuilderConfig
    {
        /*
         *  cfg：导航网格的配置（RcConfig）。
            tileX和tileZ：瓦片在网格中的坐标（X和Z轴）。
            width：沿x轴的字段宽度。[限制：>= 0] [单位：vx]。
            height：沿z轴的字段高度。[限制：>= 0] [单位：vx]。
            bmin：字段轴对齐边界框（AABB）的最小边界。[(x, y, z)] [单位：wu]。
            bmax：字段轴对齐边界框（AABB）的最大边界。[(x, y, z)] [单位：wu]。
         */
        public readonly RcConfig cfg;

        public readonly int tileX;
        public readonly int tileZ;

        /** The width of the field along the x-axis. [Limit: >= 0] [Units: vx] **/
        public readonly int width;

        /** The height of the field along the z-axis. [Limit: >= 0] [Units: vx] **/
        public readonly int height;

        /** The minimum bounds of the field's AABB. [(x, y, z)] [Units: wu] **/
        public readonly RcVec3f bmin = new RcVec3f();

        /** The maximum bounds of the field's AABB. [(x, y, z)] [Units: wu] **/
        public readonly RcVec3f bmax = new RcVec3f();

        // 构造函数：接受导航网格配置、最小边界和最大边界作为参数。这个构造函数将瓦片坐标设置为0。
        public RcBuilderConfig(RcConfig cfg, RcVec3f bmin, RcVec3f bmax) : this(cfg, bmin, bmax, 0, 0)
        {
        }
        
        // 构造函数：接受导航网格配置、最小边界、最大边界和瓦片坐标作为参数。 
        public RcBuilderConfig(RcConfig cfg, RcVec3f bmin, RcVec3f bmax, int tileX, int tileZ)
        {
            this.tileX = tileX;
            this.tileZ = tileZ;
            this.cfg = cfg;
            this.bmin = bmin;
            this.bmax = bmax;
            // 根据配置中的UseTiles属性，计算实际需要的几何数据边界（bmin和bmax）以及网格的宽度和高度。
            // 这些信息可用于查询输入几何数据，以确保导航网格瓦片在边界处正确连接，以及边界附近的障碍物与膨胀过程正确工作。
            if (cfg.UseTiles)
            {
                float tsx = cfg.TileSizeX * cfg.Cs;
                float tsz = cfg.TileSizeZ * cfg.Cs;
                this.bmin.x += tileX * tsx;
                this.bmin.z += tileZ * tsz;
                this.bmax.x = this.bmin.x + tsx;
                this.bmax.z = this.bmin.z + tsz;
                
                // Expand the heighfield bounding box by border size to find the extents of geometry we need to build this tile.
                //
                // This is done in order to make sure that the navmesh tiles connect correctly at the borders,
                // and the obstacles close to the border work correctly with the dilation process.
                // No polygons (or contours) will be created on the border area.
                //
                // IMPORTANT!
                //
                //   :''''''''':
                //   : +-----+ :
                //   : |     | :
                //   : |     |<--- tile to build
                //   : |     | :  
                //   : +-----+ :<-- geometry needed
                //   :.........:
                //
                // You should use this bounding box to query your input geometry.
                //
                // For example if you build a navmesh for terrain, and want the navmesh tiles to match the terrain tile size
                // you will need to pass in data from neighbour terrain tiles too! In a simple case, just pass in all the 8 neighbours,
                // or use the bounding box below to only pass in a sliver of each of the 8 neighbours.
                
                
                // 按边界大小扩展高度场边界框，以找到构建此图块所需的几何图形范围。
                //
                // 这样做是为了确保导航网格图块在边界处正确连接，靠近边界的障碍物可以在膨胀过程中正常工作。
                // 不会在边界区域上创建多边形（或轮廓）。
                //
                // 重要的！
                //
                // :''''''''':
                // : +-----+ :
                // : |     | :
                // : |     |<--- 平铺构建
                // : |     | :
                // : +-----+ :<-- 需要几何形状
                // :.........:
                //
                // 您应该使用此边界框来查询您的输入几何图形。
                //
                // 例如，如果您为地形构建导航网格，并希望导航网格图块匹配地形图块大小您还需要传递来自相邻地形图块的数据！ 在一个简单的情况下，只需传入所有 8 个邻居，
                // 或使用下面的边界框仅传入 8 个邻居中每一个的一小部分。
                
                this.bmin.x -= cfg.BorderSize * cfg.Cs;
                this.bmin.z -= cfg.BorderSize * cfg.Cs;
                this.bmax.x += cfg.BorderSize * cfg.Cs;
                this.bmax.z += cfg.BorderSize * cfg.Cs;
                width = cfg.TileSizeX + cfg.BorderSize * 2;
                height = cfg.TileSizeZ + cfg.BorderSize * 2;
            }
            else
            {
                RcCommons.CalcGridSize(this.bmin, this.bmax, cfg.Cs, out width, out height);
            }
        }
    }
}