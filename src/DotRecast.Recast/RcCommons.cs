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

using System;
using DotRecast.Core;

namespace DotRecast.Recast
{
    using static RcConstants;

    /// <summary>
    /// 是一个包含一些用于导航网格构建的公共静态方法的类。
    /// 这些方法主要用于在导航网格构建过程中执行一些常见的计算和操作，如计算边界框、网格大小、三角形法线等。这些方法可以在不同的构建阶段和类中使用，以提高代码的可重用性和简洁性。
    /// </summary>
    public static class RcCommons
    {
        // 是表示和处理导航网格中相邻单元格之间的方向和偏移量。
        // 当需要确定当前单元格在特定方向上的相邻单元格时，可以使用这些数组来简化计算和操作。
        private static readonly int[] DirOffsetX = { -1, 0, 1, 0, };    // 表示沿X轴方向的偏移量。数组的每个元素对应一个方向
        private static readonly int[] DirOffsetY = { 0, 1, 0, -1 };     // 表示沿Z轴方向的偏移量。数组的每个元素对应一个方向
        private static readonly int[] DirForOffset = { 3, 0, -1, 2, 1 }; // 注意不是偏移！！！ 表示由X和Z轴偏移量确定的方向。数组的每个元素对应一个由X和Z轴偏移量组合确定的方向

        /// Sets the neighbor connection data for the specified direction.
        /// 设置指定方向的邻居连接数据。 
        /// @param[in]		span			The span to update.        需要更新的跨度（RcCompactSpan）
        /// @param[in]		direction		The direction to set. [Limits: 0 <= value < 4]    要设置的方向。[限制：0 <= value < 4]
        /// @param[in]		neighborIndex	The index of the neighbor span.                   邻居跨度的索引
        public static void SetCon(RcCompactSpan span, int direction, int neighborIndex)
        {
            // 计算位移值
            int shift = direction * 6;
            // 获取当前跨度的连接数据
            int con = span.con;
            // 更新跨度的连接数据
            span.con = (con & ~(0x3f << shift)) | ((neighborIndex & 0x3f) << shift);
        }

        /// Gets neighbor connection data for the specified direction.
        /// 获取指定方向的邻居连接数据。
        /// @param[in]		span		The span to check.        需要检查的跨度（RcCompactSpan）    
        /// @param[in]		direction	The direction to check. [Limits: 0 <= value < 4]    要检查的方向。[限制：0 <= value < 4]
        /// @return The neighbor connection data for the specified direction, or #RC_NOT_CONNECTED if there is no connection.  指定方向的邻居连接数据，如果没有连接则为#RC_NOT_CONNECTED。   
        public static int GetCon(RcCompactSpan s, int dir)
        {
            // 计算位移值
            int shift = dir * 6;
            // 获取并返回指定方向的邻居连接数据
            return (s.con >> shift) & 0x3f;
        }

        /// Gets the standard width (x-axis) offset for the specified direction.
        /// 获取指定方向的标准宽度（x轴）偏移。  
        /// @param[in]		direction		The direction. [Limits: 0 <= value < 4]        要获取偏移量的方向。[限制：0 <= value < 4]  
        /// @return The width offset to apply to the current cell position to move in the direction. 应用于当前单元格位置以沿该方向移动的宽度偏移。
        public static int GetDirOffsetX(int dir)
        {
            // 使用按位与操作符将dir与0x03 (0011)进行按位与运算 【起到限制取值范围的作用】，然后从DirOffsetX数组中获取并返回相应的宽度偏移量
            return DirOffsetX[dir & 0x03];
        }

        // TODO (graham): Rename this to rcGetDirOffsetZ
        /// Gets the standard height (z-axis) offset for the specified direction.
        /// 获取指定方向的标准高度（z轴）偏移
        /// @param[in]		direction		The direction. [Limits: 0 <= value < 4]         要获取偏移量的方向。[限制：0 <= value < 4]  
        /// @return The height offset to apply to the current cell position to move in the direction.  应用于当前单元格位置以沿该方向移动的高度偏移。
        public static int GetDirOffsetY(int dir)
        {
            return DirOffsetY[dir & 0x03];
        }

        // 注意不是偏移！！！结果是方向
        /// Gets the direction for the specified offset. One of x and y should be 0.
        /// 此方法的主要目的是在导航网格构建过程中根据x轴和z轴的偏移量获取相应的方向，以便在处理相邻单元格和计算路径时使用正确的方向。
        /// 获取指定偏移的方向。注意：x和y中的一个应该为0，表示只在一个轴上有偏移。
        /// @param[in]		offsetX		The x offset. [Limits: -1 <= value <= 1]     x轴偏移。[限制：-1 <= value <= 1]
        /// @param[in]		offsetZ		The z offset. [Limits: -1 <= value <= 1]     z轴偏移。[限制：-1 <= value <= 1]
        /// @return The direction that represents the offset.    表示偏移的方向。 
        public static int GetDirForOffset(int x, int y)
        {
            // 使用公式((y + 1) << 1) + x 计算在DirForOffset数组中的索引。   注意公式结果是索引。
            /*
             *  数组DirForOffset中每个元素的含义： 3*3共9个去掉都不为0的剩下5个
                DirForOffset[0] = 3：表示向左（负X轴方向）的偏移。索引0对应于offsetX = -1和offsetZ = 0。
                DirForOffset[1] = 0：表示向上（正Z轴方向）的偏移。索引1对应于offsetX = 0和offsetZ = -1。
                DirForOffset[2] = -1：表示无效的方向。索引2对应于offsetX = 1和offsetZ = -1，这是一个无效的偏移组合，因为X和Z轴上都有偏移。
                DirForOffset[3] = 2：表示向右（正X轴方向）的偏移。索引3对应于offsetX = -1和offsetZ = 0。
                DirForOffset[4] = 1：表示向下（负Z轴方向）的偏移。索引4对应于offsetX = 0和offsetZ = 0。
             */
            return DirForOffset[((y + 1) << 1) + x];
        }

        /// <summary>
        ///  用于计算顶点集的边界框（AABB，Axis-Aligned Bounding Box）
        /// </summary>
        /// verts：顶点集，包含顶点坐标的浮点数数组。   [每3个值作为一个顶点的xyz]
        /// nv：顶点集中的顶点数量。     应该是 verts 的 1/3 ?
        /// bmin：计算得到的边界框的最小边界（输出参数）。
        /// bmax：计算得到的边界框的最大边界（输出参数）。
        public static void CalcBounds(float[] verts, int nv, float[] bmin, float[] bmax)
        {
            // 初始化bmin和bmax数组为顶点集中的第一个顶点坐标
            for (int i = 0; i < 3; i++)
            {
                bmin[i] = verts[i];
                bmax[i] = verts[i];
            }

            // 遍历顶点集中的其他顶点，对于每个顶点： a. 遍历其x、y和z坐标，将bmin和bmax数组中的对应值更新为当前顶点坐标和原始值之间的最小值和最大值。
            for (int i = 1; i < nv; ++i)
            {
                for (int j = 0; j < 3; j++)
                {
                    bmin[j] = Math.Min(bmin[j], verts[i * 3 + j]);
                    bmax[j] = Math.Max(bmax[j], verts[i * 3 + j]);
                }
            }
            // Calculate bounding box.
        }

        // 用于计算导航网格的大小
        /*
         *  bmin：边界框的最小边界（RcVec3f）。
            bmax：边界框的最大边界（RcVec3f）。
            cs：单元格大小（浮点数）。
            sizeX：计算得到的网格宽度（输出参数，整数）。      所以这些高度宽度的单位是单元格大小？
            sizeZ：计算得到的网格高度（输出参数，整数）。
         */
        public static void CalcGridSize(RcVec3f bmin, RcVec3f bmax, float cs, out int sizeX, out int sizeZ)
        {
            sizeX = (int)((bmax.x - bmin.x) / cs + 0.5f);
            sizeZ = (int)((bmax.z - bmin.z) / cs + 0.5f);
        }


        // 用于计算导航网格中的瓦片数量
        /*
         *  bmin：边界框的最小边界（RcVec3f）。
            bmax：边界框的最大边界（RcVec3f）。
            cs：单元格大小（浮点数）。                  单元格跟瓦片不是一个东西？
            tileSizeX：瓦片的宽度（整数）。            单位应该也是基于单元格的？
            tileSizeZ：瓦片的高度（整数）。
            tw：计算得到的瓦片数量（宽度方向）（输出参数，整数）。
            td：计算得到的瓦片数量（高度方向）（输出参数，整数）。
         */
        public static void CalcTileCount(RcVec3f bmin, RcVec3f bmax, float cs, int tileSizeX, int tileSizeZ, out int tw, out int td)
        {
            // 方法计算网格的宽度和高度
            CalcGridSize(bmin, bmax, cs, out var gw, out var gd);
            tw = (gw + tileSizeX - 1) / tileSizeX;
            td = (gd + tileSizeZ - 1) / tileSizeZ;
        }

        /// @par  标记可行走的三角形
        ///三角形的坡度角为 a，我们利用向量叉乘 v2v1x v2v3得到三角平面的法向量，然后对法向量归一化成单位法向量，那么此时单位法向量的Y坐标就是坡度角的余弦值。当单位法向量的Y坐标大于最大可行走坡度角的余弦值时，说明该三角平面是可行走的。
        /// 
        /// 标记具有小于指定值斜率的所有三角形的区域ID。
        /// 主要目的是在导航网格构建过程中根据可行走斜率角度标记三角形的区域ID，以便在后续步骤中正确处理可行走和不可行走的区域。
        /// Modifies the area id of all triangles with a slope below the specified value.  修改斜率低于指定值的所有三角形的面积 id。
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.  有关配置参数的更多信息，请参阅#rcConfig 文档。
        ///
        /// @see rcHeightfield, rcClearUnwalkableTriangles, rcRasterizeTriangles
        /*
         *  ctx：RcTelemetry对象，用于记录和报告构建过程中的警告和错误。
            walkableSlopeAngle：可行走斜面的最大斜率角度（浮点数，单位：度）。
            verts：顶点集，包含顶点坐标的浮点数数组。
            tris：三角形顶点索引数组。   每3个元素一组
            nt：三角形数量。    应该是tris的 1/3 ?  
            areaMod：RcAreaModification对象，用于修改区域ID。
         */
        public static int[] MarkWalkableTriangles(RcTelemetry ctx, float walkableSlopeAngle, float[] verts, int[] tris, int nt, RcAreaModification areaMod)
        {
            // 初始化一个长度为nt的整数数组areas，用于存储每个三角形的区域ID。
            int[] areas = new int[nt];
            // 计算可行走斜率的阈值
            float walkableThr = (float)Math.Cos(walkableSlopeAngle / 180.0f * Math.PI);
            RcVec3f norm = new RcVec3f();
            
            // 遍历所有三角形
            for (int i = 0; i < nt; ++i)
            {
                int tri = i * 3;
                // 计算三角形的法线
                CalcTriNormal(verts, tris[tri], tris[tri + 1], tris[tri + 2], ref norm);
                // Check if the face is walkable.
                //  检查三角形是否可行走（法线的y分量大于阈值）
                if (norm.y > walkableThr)
                    areas[i] = areaMod.Apply(areas[i]);  // 如果可行走，将三角形的区域ID修改为areaMod应用后的值
            }

            return areas;
        }

        /// <summary>
        /// 计算三角形的法线。
        /// 这个方法的主要目的是在导航网格构建过程中计算三角形的法线，以便在后续步骤中判断三角形是否可行走（根据法线的Y分量和可行走斜率阈值）。
        /// 计算法线的过程是基于向量叉积的几何性质，即两个向量的叉积结果是一个垂直于这两个向量所在平面的向量。
        /// </summary>
        /*
         *  verts：顶点集，包含顶点坐标的浮点数数组。
            v0、v1、v2：三角形的三个顶点在顶点集中的索引。
            norm：计算得到的三角形法线（输出参数，RcVec3f）。
         */
        public static void CalcTriNormal(float[] verts, int v0, int v1, int v2, ref RcVec3f norm)
        {
            RcVec3f e0 = new RcVec3f();
            RcVec3f e1 = new RcVec3f();
            RcVec3f.Sub(ref e0, verts, v1 * 3, v0 * 3);
            RcVec3f.Sub(ref e1, verts, v2 * 3, v0 * 3);
            // 计算法线norm为向量e0和e1的叉积
            RcVec3f.Cross(ref norm, e0, e1);
            RcVec3f.Normalize(ref norm);
        }


        /// @par
        /// 仅设置不可行走三角形的区域ID，不改变可行走三角形的区域ID。 
        /// Only sets the area id's for the unwalkable triangles. Does not alter the area id's for walkable triangles.
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.  有关配置参数的更多信息，请参阅#rcConfig 文档。
        ///
        /// @see rcHeightfield, rcClearUnwalkableTriangles, rcRasterizeTriangles
        /*
         *  ctx：RcTelemetry对象，用于记录和报告构建过程中的警告和错误。
            walkableSlopeAngle：可行走斜面的最大斜率角度（浮点数，单位：度）。
            verts：顶点集，包含顶点坐标的浮点数数组。
            nv：顶点集中的顶点数量。
            tris：三角形顶点索引数组。
            nt：三角形数量。
            areas：三角形区域ID数组。
         */
        public static void ClearUnwalkableTriangles(RcTelemetry ctx, float walkableSlopeAngle, float[] verts, int nv, int[] tris, int nt, int[] areas)
        {
            // 计算可行走斜率的阈值
            float walkableThr = (float)Math.Cos(walkableSlopeAngle / 180.0f * Math.PI);

            // 用于存储三角形法线
            RcVec3f norm = new RcVec3f();

            // 遍历所有三角形
            for (int i = 0; i < nt; ++i)
            {
                int tri = i * 3;
                // 计算三角形的法线
                CalcTriNormal(verts, tris[tri], tris[tri + 1], tris[tri + 2], ref norm);
                // Check if the face is walkable.
                // 检查三角形是否不可行走（法线的y分量小于等于阈值）
                if (norm.y <= walkableThr)   
                    areas[i] = RC_NULL_AREA;   // 如果不可行走，将三角形的区域ID设置为RC_NULL_AREA
            }
        }
    }
}