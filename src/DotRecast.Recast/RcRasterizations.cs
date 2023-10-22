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
using static DotRecast.Recast.RcConstants;

namespace DotRecast.Recast
{
    // 包含了将三角形栅格化到高度场（heightfield）中的方法。
    // 在导航网格生成过程中，高度场是一个重要的中间数据结构，用于存储场景的高度信息。
    // 这个类提供了将三角形几何数据转换为高度场的方法，从而为后续的导航网格生成做准备。
    
    // 这个类提供了将三角形几何数据转换为高度场的方法，为后续的导航网格生成做准备。
    // 这些方法在导航网格生成过程中非常关键，因为它们将场景的几何信息转换为更适合用于寻路的数据结构。
    public static class RcRasterizations
    {
        /**
         * Check whether two bounding boxes overlap 检查两个边界框是否重叠
         *
         * @param amin
         *            Min axis extents of bounding box A  边界框 A 的最小轴范围
         * @param amax
         *            Max axis extents of bounding box A  边界框 A 的最小轴范围
         * @param bmin
         *            Min axis extents of bounding box B   边界框 B 的最小轴范围
         * @param bmax
         *            Max axis extents of bounding box B   边界框 B 的最小轴范围
         * @returns true if the two bounding boxes overlap. False otherwise  @如果两个边界框重叠则返回 true。 否则为假
         */
        // 检查两个边界框是否重叠。这个方法用于判断三角形是否与高度场的边界框相交，从而决定是否需要将三角形栅格化到高度场中。
        private static bool OverlapBounds(float[] amin, float[] amax, float[] bmin, float[] bmax)
        {
            bool overlap = true;
            overlap = (amin[0] > bmax[0] || amax[0] < bmin[0]) ? false : overlap;
            overlap = (amin[1] > bmax[1] || amax[1] < bmin[1]) ? false : overlap;
            overlap = (amin[2] > bmax[2] || amax[2] < bmin[2]) ? false : overlap;
            return overlap;
        }

        private static bool OverlapBounds(RcVec3f amin, RcVec3f amax, RcVec3f bmin, RcVec3f bmax)
        {
            bool overlap = true;
            overlap = (amin.x > bmax.x || amax.x < bmin.x) ? false : overlap;
            overlap = (amin.y > bmax.y || amax.y < bmin.y) ? false : overlap;
            overlap = (amin.z > bmax.z || amax.z < bmin.z) ? false : overlap;
            return overlap;
        }


        /// Adds a span to the heightfield.  If the new span overlaps existing spans,
        /// it will merge the new span with the existing ones.
        /// 将跨度添加到高度字段。 如果新的跨度与现有跨度重叠，
        /// 它将把新的跨度与现有的跨度合并。
        ///
        /// @param[in]	heightfield		    Heightfield to add spans to  .     heightfield 要添加跨度的高度字段     
        /// @param[in]	x					The new span's column cell x index      x 新span的列单元格 x 索引
        /// @param[in]	z					The new span's column cell z index      z 新span的列单元格 z 索引
        /// @param[in]	min					The new span's minimum cell index      min 新span的最小单元格索引
        /// @param[in]	max					The new span's maximum cell index     max 新span的最大单元格索引
        /// @param[in]	areaID				The new span's area type ID         ] areaID 新span的区域类型ID
        /// @param[in]	flagMergeThreshold	How close two spans maximum extents need to be to merge area type IDs . flagMergeThreshold 两个跨度最大范围需要有多接近才能合并区域类型 ID
        ///  向高度场中添加一个跨度（span）。跨度是高度场中的一个基本单位，表示一个网格单元在垂直方向上的高度范围。如果新添加的跨度与现有的跨度重叠，它们将被合并。
        public static void AddSpan(RcHeightfield heightfield, int x, int y, int spanMin, int spanMax, int areaId, int flagMergeThreshold)
        {
            int idx = x + y * heightfield.width;

            RcSpan s = new RcSpan();
            s.smin = spanMin;
            s.smax = spanMax;
            s.area = areaId;
            s.next = null;

            // Empty cell, add the first span.  空单元格，添加第一个跨度。
            if (heightfield.spans[idx] == null)
            {
                heightfield.spans[idx] = s;
                return;
            }

            RcSpan prev = null;
            RcSpan cur = heightfield.spans[idx];

            // Insert and merge spans.       插入并合并跨度。
            while (cur != null)
            {
                if (cur.smin > s.smax)
                {
                    // Current span is further than the new span, break.      当前跨度比新跨度更远，break。
                    break;
                }
                else if (cur.smax < s.smin)
                {
                    // Current span is before the new span advance.      当前跨度在新跨度前进之前。
                    prev = cur;
                    cur = cur.next;
                }
                else
                {
                    // Merge spans.     合并跨度。
                    if (cur.smin < s.smin)
                        s.smin = cur.smin;
                    if (cur.smax > s.smax)
                        s.smax = cur.smax;

                    // Merge flags.           合并标志。
                    if (Math.Abs(s.smax - cur.smax) <= flagMergeThreshold)
                        s.area = Math.Max(s.area, cur.area);

                    // Remove current span.       删除当前跨度。
                    RcSpan next = cur.next;
                    if (prev != null)
                        prev.next = next;
                    else
                        heightfield.spans[idx] = next;
                    cur = next;
                }
            }

            // Insert new span.       插入新的跨度。
            if (prev != null)
            {
                s.next = prev.next;
                prev.next = s;
            }
            else
            {
                s.next = heightfield.spans[idx];
                heightfield.spans[idx] = s;
            }
        }

        /// Divides a convex polygon of max 12 vertices into two convex polygons across a separating axis.
        ///  将最多 12 个顶点的凸多边形沿分离轴划分为两个凸多边形。
        /// 
        /// @param[in]	inVerts			The input polygon vertices  输入多边形顶点
        /// @param[in]	inVertsCount	The number of input polygon vertices   输入多边形顶点数量
        /// @param[out]	outVerts1		Resulting polygon 1's vertices       结果多边形 1 的顶点
        /// @param[out]	outVerts1Count	The number of resulting polygon 1 vertices  生成的多边形 1 顶点的数量
        /// @param[out]	outVerts2		Resulting polygon 2's vertices     结果多边形 2 的顶点
        /// @param[out]	outVerts2Count	The number of resulting polygon 2 vertices    生成的多边形2个顶点的数量
        /// @param[in]	axisOffset		THe offset along the specified axis      沿指定轴的偏移量
        /// @param[in]	axis			The separating axis                  分离轴
        /// 将一个凸多边形沿一个分隔轴划分为两个凸多边形。这个方法用于将三角形划分为多个网格单元。
        private static void DividePoly(float[] inVerts, int inVertsOffset, int inVertsCount,
            int outVerts1, out int outVerts1Count,
            int outVerts2, out int outVerts2Count,
            float axisOffset, int axis)
        {
            float[] d = new float[12];

            // How far positive or negative away from the separating axis is each vertex.
            // 每个顶点距分离轴的正负距离是多少。
            for (int inVert = 0; inVert < inVertsCount; ++inVert)
            {
                d[inVert] = axisOffset - inVerts[inVertsOffset + inVert * 3 + axis];
            }

            int poly1Vert = 0;
            int poly2Vert = 0;
            for (int inVertA = 0, inVertB = inVertsCount - 1; inVertA < inVertsCount; inVertB = inVertA, ++inVertA)
            {
                bool ina = d[inVertB] >= 0;
                bool inb = d[inVertA] >= 0;
                if (ina != inb)
                {
                    float s = d[inVertB] / (d[inVertB] - d[inVertA]);
                    inVerts[outVerts1 + poly1Vert * 3 + 0] = inVerts[inVertsOffset + inVertB * 3 + 0] +
                                                             (inVerts[inVertsOffset + inVertA * 3 + 0] - inVerts[inVertsOffset + inVertB * 3 + 0]) * s;
                    inVerts[outVerts1 + poly1Vert * 3 + 1] = inVerts[inVertsOffset + inVertB * 3 + 1] +
                                                             (inVerts[inVertsOffset + inVertA * 3 + 1] - inVerts[inVertsOffset + inVertB * 3 + 1]) * s;
                    inVerts[outVerts1 + poly1Vert * 3 + 2] = inVerts[inVertsOffset + inVertB * 3 + 2] +
                                                             (inVerts[inVertsOffset + inVertA * 3 + 2] - inVerts[inVertsOffset + inVertB * 3 + 2]) * s;
                    RcVec3f.Copy(inVerts, outVerts2 + poly2Vert * 3, inVerts, outVerts1 + poly1Vert * 3);
                    poly1Vert++;
                    poly2Vert++;
                    // add the i'th point to the right polygon. Do NOT add points that are on the dividing line since these were already added above
                    // 将第 i 个点添加到右侧多边形。 不要添加分界线上的点，因为这些点已经在上面添加了
                    if (d[inVertA] > 0)
                    {
                        RcVec3f.Copy(inVerts, outVerts1 + poly1Vert * 3, inVerts, inVertsOffset + inVertA * 3);
                        poly1Vert++;
                    }
                    else if (d[inVertA] < 0)
                    {
                        RcVec3f.Copy(inVerts, outVerts2 + poly2Vert * 3, inVerts, inVertsOffset + inVertA * 3);
                        poly2Vert++;
                    }
                }
                else // same side   同边
                {
                    // add the i'th point to the right polygon. Addition is done even for points on the dividing line
                    // 将第 i 个点添加到右侧多边形。 即使分界线上的点也进行加法
                    if (d[inVertA] >= 0)
                    {
                        RcVec3f.Copy(inVerts, outVerts1 + poly1Vert * 3, inVerts, inVertsOffset + inVertA * 3);
                        poly1Vert++;
                        if (d[inVertA] != 0)
                            continue;
                    }

                    RcVec3f.Copy(inVerts, outVerts2 + poly2Vert * 3, inVerts, inVertsOffset + inVertA * 3);
                    poly2Vert++;
                }
            }

            outVerts1Count = poly1Vert;
            outVerts2Count = poly2Vert;
        }

        ///	Rasterize a single triangle to the heightfield.
        /// 将单个三角形光栅化到高度场。
        ///	This code is extremely hot, so much care should be given to maintaining maximum perf here.
        /// 这段代码非常热，所以应该非常小心地保持这里的最大性能。
        /// @param[in] 	v0					Triangle vertex 0   三角形顶点 0
        /// @param[in] 	v1					Triangle vertex 1    三角形顶点 1
        /// @param[in] 	v2					Triangle vertex 2   三角形顶点 2
        /// @param[in] 	areaID				The area ID to assign to the rasterized spans  分配给栅格化跨度的区域 ID
        /// @param[in] 	heightfield			Heightfield to rasterize into  要栅格化的高度字段
        /// @param[in] 	heightfieldBBMin	The min extents of the heightfield bounding box  高度场边界框的最小范围
        /// @param[in] 	heightfieldBBMax	The max extents of the heightfield bounding box   heightfield 边界框的最大范围
        /// @param[in] 	cellSize			The x and z axis size of a voxel in the heightfield   高度场中体素的 x 和 z 轴大小
        /// @param[in] 	inverseCellSize		1 / cellSize
        /// @param[in] 	inverseCellHeight	1 / cellHeight
        /// @param[in] 	flagMergeThreshold	The threshold in which area flags will be merged
        ///        区域标志将被合并的阈值
        /// @returns true if the operation completes successfully.  false if there was an error adding spans to the heightfield.
        ///     如果操作成功完成则返回 true。 如果向高度字段添加跨度时发生错误，则返回 false。
        /// 将一个三角形栅格化到指定的高度场中。这个方法首先计算三角形的边界框，然后将三角形划分为多个网格单元，最后将跨度添加到高度场中。
        private static void RasterizeTri(float[] verts, int v0, int v1, int v2, int area, RcHeightfield heightfield,
            RcVec3f heightfieldBBMin, RcVec3f heightfieldBBMax,
            float cellSize, float inverseCellSize, float inverseCellHeight,
            int flagMergeThreshold)
        {
            RcVec3f tmin = new RcVec3f();
            RcVec3f tmax = new RcVec3f();
            float by = heightfieldBBMax.y - heightfieldBBMin.y;

            // Calculate the bounding box of the triangle.
            // 计算三角形的边界框。
            RcVec3f.Copy(ref tmin, verts, v0 * 3);
            RcVec3f.Copy(ref tmax, verts, v0 * 3);
            tmin.Min(verts, v1 * 3);
            tmin.Min(verts, v2 * 3);
            tmax.Max(verts, v1 * 3);
            tmax.Max(verts, v2 * 3);

            // If the triangle does not touch the bbox of the heightfield, skip the triagle.
            // 如果三角形没有触及高度场的bbox，则跳过三角形。
            if (!OverlapBounds(heightfieldBBMin, heightfieldBBMax, tmin, tmax))
                return;

            // Calculate the footprint of the triangle on the grid's y-axis
            // 计算三角形在网格 y 轴上的足迹
            int z0 = (int)((tmin.z - heightfieldBBMin.z) * inverseCellSize);
            int z1 = (int)((tmax.z - heightfieldBBMin.z) * inverseCellSize);

            int w = heightfield.width;
            int h = heightfield.height;
            // use -1 rather than 0 to cut the polygon properly at the start of the tile
            // 使用 -1 而不是 0 在图块的开头正确切割多边形
            z0 = Math.Clamp(z0, -1, h - 1);
            z1 = Math.Clamp(z1, 0, h - 1);

            // Clip the triangle into all grid cells it touches.
            // 将三角形剪裁到它接触的所有网格单元中。
            float[] buf = new float[7 * 3 * 4];
            int @in = 0;
            int inRow = 7 * 3;
            int p1 = inRow + 7 * 3;
            int p2 = p1 + 7 * 3;

            RcVec3f.Copy(buf, 0, verts, v0 * 3);
            RcVec3f.Copy(buf, 3, verts, v1 * 3);
            RcVec3f.Copy(buf, 6, verts, v2 * 3);
            int nvRow, nvIn = 3;

            for (int z = z0; z <= z1; ++z)
            {
                // Clip polygon to row. Store the remaining polygon as well
                // 将多边形剪切到行。 也存储剩余的多边形
                float cellZ = heightfieldBBMin.z + z * cellSize;
                DividePoly(buf, @in, nvIn, inRow, out nvRow, p1, out nvIn, cellZ + cellSize, 2);
                (@in, p1) = (p1, @in);

                if (nvRow < 3)
                    continue;

                if (z < 0)
                {
                    continue;
                }

                // find the horizontal bounds in the row
                // 找到行中的水平边界
                float minX = buf[inRow], maxX = buf[inRow];
                for (int i = 1; i < nvRow; ++i)
                {
                    float v = buf[inRow + i * 3];
                    minX = Math.Min(minX, v);
                    maxX = Math.Max(maxX, v);
                }

                int x0 = (int)((minX - heightfieldBBMin.x) * inverseCellSize);
                int x1 = (int)((maxX - heightfieldBBMin.x) * inverseCellSize);
                if (x1 < 0 || x0 >= w)
                {
                    continue;
                }

                x0 = Math.Clamp(x0, -1, w - 1);
                x1 = Math.Clamp(x1, 0, w - 1);

                int nv, nv2 = nvRow;
                for (int x = x0; x <= x1; ++x)
                {
                    // Clip polygon to column. store the remaining polygon as well
                    // 将多边形裁剪到列。 也存储剩余的多边形
                    float cx = heightfieldBBMin.x + x * cellSize;
                    DividePoly(buf, inRow, nv2, p1, out nv, p2, out nv2, cx + cellSize, 0);
                    (inRow, p2) = (p2, inRow);

                    if (nv < 3)
                        continue;

                    if (x < 0)
                    {
                        continue;
                    }

                    // Calculate min and max of the span.
                    // 计算跨度的最小值和最大值。
                    float spanMin = buf[p1 + 1];
                    float spanMax = buf[p1 + 1];
                    for (int i = 1; i < nv; ++i)
                    {
                        spanMin = Math.Min(spanMin, buf[p1 + i * 3 + 1]);
                        spanMax = Math.Max(spanMax, buf[p1 + i * 3 + 1]);
                    }

                    spanMin -= heightfieldBBMin.y;
                    spanMax -= heightfieldBBMin.y;
                    // Skip the span if it is outside the heightfield bbox
                    // 如果跨度超出高度域 bbox，则跳过该跨度
                    if (spanMax < 0.0f)
                        continue;
                    if (spanMin > by)
                        continue;
                    // Clamp the span to the heightfield bbox.
                    // 将跨度固定到高度域 bbox。
                    if (spanMin < 0.0f)
                        spanMin = 0;
                    if (spanMax > by)
                        spanMax = by;

                    // Snap the span to the heightfield height grid.
                    // 将跨度捕捉到高度场高度网格。
                    int spanMinCellIndex = Math.Clamp((int)Math.Floor(spanMin * inverseCellHeight), 0, SPAN_MAX_HEIGHT);
                    int spanMaxCellIndex = Math.Clamp((int)Math.Ceiling(spanMax * inverseCellHeight), spanMinCellIndex + 1, SPAN_MAX_HEIGHT);

                    AddSpan(heightfield, x, z, spanMinCellIndex, spanMaxCellIndex, area, flagMergeThreshold);
                }
            }
        }

        /**
     * Rasterizes a single triangle into the specified heightfield. Calling this for each triangle in a mesh is less
     * efficient than calling rasterizeTriangles. No spans will be added if the triangle does not overlap the
     * heightfield grid.
         *  将单个三角形光栅化到指定的高度场中。 对网格中的每个三角形调用此方法的效率低于调用 rasterizeTriangles。 如果三角形不与高度场网格重叠，则不会添加跨度。
     *
     * @param heightfield
     *            An initialized heightfield.      初始化的高度场。
     * @param verts
     *            An array with vertex coordinates [(x, y, z) * N]      顶点坐标数组 [(x, y, z) * N]
     * @param v0
     *            Index of triangle vertex 0, will be multiplied by 3 to get vertex coordinates    三角形顶点0的索引，将乘以3得到顶点坐标
     * @param v1
     *            Triangle vertex 1 index      三角形顶点1索引
     * @param v2
     *            Triangle vertex 2 index       三角形顶点2索引
     * @param areaId
     *            The area id of the triangle. [Limit: <= WALKABLE_AREA)       三角形的面积ID。 [限制：<= WALKABLE_AREA）
     * @param flagMergeThreshold
     *            The distance where the walkable flag is favored over the non-walkable flag. [Limit: >= 0] [Units: vx]  步行标志优于非步行标志的距离。 [限制：>= 0] [单位：vx]
     * @see Heightfield
     */
        // 将一个索引三角形网格栅格化到指定的高度场中。这个方法遍历三角形网格中的每个三角形，并调用 RasterizeTri 方法将其栅格化到高度场中。 
        public static void RasterizeTriangle(RcHeightfield heightfield, float[] verts, int v0, int v1, int v2, int area,
            int flagMergeThreshold, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_TRIANGLES);

            float inverseCellSize = 1.0f / heightfield.cs;
            float inverseCellHeight = 1.0f / heightfield.ch;
            RasterizeTri(verts, v0, v1, v2, area, heightfield, heightfield.bmin, heightfield.bmax, heightfield.cs, inverseCellSize,
                inverseCellHeight, flagMergeThreshold);
        }

        /**
     * Rasterizes an indexed triangle mesh into the specified heightfield. Spans will only be added for triangles that
     * overlap the heightfield grid.
         * 将索引三角形网格栅格化到指定的高度字段中。 仅对与高度场网格重叠的三角形添加跨度。
     *
     * @param heightfield
     *            An initialized heightfield.          初始化的高度场。
     * @param verts
     *            The vertices. [(x, y, z) * N]        顶点。 [（x，y，z）* N]
     * @param tris
     *            The triangle indices. [(vertA, vertB, vertC) * nt]       三角形指数。 [(vertA, vertB, vertC) * nt]
     * @param areaIds
     *            The area id's of the triangles. [Limit: <= WALKABLE_AREA] [Size: numTris]         三角形的面积 ID。 [限制：<= WALKABLE_AREA] [大小：numTris]
     * @param numTris
     *            The number of triangles.            三角形的数量。
     * @param flagMergeThreshold
     *            The distance where the walkable flag is favored over the non-walkable flag. [Limit: >= 0] [Units: vx]      步行标志优于非步行标志的距离。 [限制：>= 0] [单位：vx]
     * @see Heightfield
     */
        // 将一个三角形列表栅格化到指定的高度场中。这个方法与 RasterizeTriangle 类似，但接受的输入是一个三角形顶点列表，而不是索引三角形网格。
        public static void RasterizeTriangles(RcHeightfield heightfield, float[] verts, int[] tris, int[] areaIds, int numTris,
            int flagMergeThreshold, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_TRIANGLES);

            float inverseCellSize = 1.0f / heightfield.cs;
            float inverseCellHeight = 1.0f / heightfield.ch;
            for (int triIndex = 0; triIndex < numTris; ++triIndex)
            {
                int v0 = tris[triIndex * 3 + 0];
                int v1 = tris[triIndex * 3 + 1];
                int v2 = tris[triIndex * 3 + 2];
                RasterizeTri(verts, v0, v1, v2, areaIds[triIndex], heightfield, heightfield.bmin, heightfield.bmax, heightfield.cs,
                    inverseCellSize, inverseCellHeight, flagMergeThreshold);
            }
        }

        /**
     * Rasterizes a triangle list into the specified heightfield. Expects each triangle to be specified as three
     * sequential vertices of 3 floats. Spans will only be added for triangles that overlap the heightfield grid.
         * 将三角形列表光栅化到指定的高度域中。 期望将每个三角形指定为 3 个浮点数的三个连续顶点。 仅对与高度场网格重叠的三角形添加跨度。
     *
     * @param heightfield
     *            An initialized heightfield.        初始化的高度场。
     * @param verts
     *            The vertices. [(x, y, z) * numVerts]       顶点。 [(x, y, z) * numVerts]
     * @param areaIds
     *            The area id's of the triangles. [Limit: <= WALKABLE_AREA] [Size: numTris]       三角形的面积 ID。 [限制：<= WALKABLE_AREA] [大小：numTris]
     * @param tris
     *            The triangle indices. [(vertA, vertB, vertC) * nt]                   三角形指数。 [(vertA, vertB, vertC) * nt]
     * @param numTris
     *            The number of triangles.                     三角形的数量。
     * @param flagMergeThreshold
     *            The distance where the walkable flag is favored over the non-walkable flag. [Limit: >= 0] [Units: vx]       步行标志优于非步行标志的距离。 [限制：>= 0] [单位：vx]
     * @see Heightfield
     */
        public static void RasterizeTriangles(RcHeightfield heightfield, float[] verts, int[] areaIds, int numTris,
            int flagMergeThreshold, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_TRIANGLES);

            float inverseCellSize = 1.0f / heightfield.cs;
            float inverseCellHeight = 1.0f / heightfield.ch;
            for (int triIndex = 0; triIndex < numTris; ++triIndex)
            {
                int v0 = (triIndex * 3 + 0);
                int v1 = (triIndex * 3 + 1);
                int v2 = (triIndex * 3 + 2);
                RasterizeTri(verts, v0, v1, v2, areaIds[triIndex], heightfield, heightfield.bmin, heightfield.bmax, heightfield.cs,
                    inverseCellSize, inverseCellHeight, flagMergeThreshold);
            }
        }
    }
}