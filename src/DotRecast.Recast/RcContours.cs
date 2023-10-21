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
using System.Collections.Generic;
using DotRecast.Core;

namespace DotRecast.Recast
{
    using static RcConstants;
    using static RcCommons;

    /// <summary>
    /// 一个公共静态类，主要用于处理和简化轮廓。可以用于导航网格生成、地形分析等场景。
    /// </summary>
    public static class RcContours
    {
        // 用于获取指定位置（x, y）和方向 dir的角点高度。同时，它还会检查顶点是否为特殊边缘顶点，这些顶点稍后将被删除。
        /*
         *  int x, int y：二维坐标，表示当前处理的点的位置。
            int i：当前处理点在压缩高度场chf中的索引。
            int dir：表示当前处理方向的整数，范围为0到3。
            RcCompactHeightfield chf：一个压缩高度场对象，包含地形的高度信息和连接信息。
            out bool isBorderVertex：输出参数，表示当前顶点是否为特殊边缘顶点。
         */
        private static int GetCornerHeight(int x, int y, int i, int dir, RcCompactHeightfield chf, out bool isBorderVertex)
        {
            // 初始化一些变量，如角点高度ch，下一个方向dirp，
            isBorderVertex = false;

            RcCompactSpan s = chf.spans[i];
            int ch = s.y;
            int dirp = (dir + 1) & 0x3;

            // 以及一个长度为4的整数数组regs，用于存储当前点和相邻点的区域和区域代码。
            int[] regs =
            {
                0, 0, 0, 0
            };

            // Combine region and area codes in order to prevent
            // border vertices which are in between two areas to be removed.
            // 组合区域和区域代码以防止两个区域之间的边界顶点被删除。
            regs[0] = chf.spans[i].reg | (chf.areas[i] << 16);

            // 遍历当前点的相邻点，更新角点高度ch和相邻点的区域和区域代码。
            if (GetCon(s, dir) != RC_NOT_CONNECTED)
            {
                int ax = x + GetDirOffsetX(dir);
                int ay = y + GetDirOffsetY(dir);
                int ai = chf.cells[ax + ay * chf.width].index + GetCon(s, dir);
                RcCompactSpan @as = chf.spans[ai];
                ch = Math.Max(ch, @as.y);
                regs[1] = chf.spans[ai].reg | (chf.areas[ai] << 16);
                if (GetCon(@as, dirp) != RC_NOT_CONNECTED)
                {
                    int ax2 = ax + GetDirOffsetX(dirp);
                    int ay2 = ay + GetDirOffsetY(dirp);
                    int ai2 = chf.cells[ax2 + ay2 * chf.width].index + GetCon(@as, dirp);
                    RcCompactSpan as2 = chf.spans[ai2];
                    ch = Math.Max(ch, as2.y);
                    regs[2] = chf.spans[ai2].reg | (chf.areas[ai2] << 16);
                }
            }

            if (GetCon(s, dirp) != RC_NOT_CONNECTED)
            {
                int ax = x + GetDirOffsetX(dirp);
                int ay = y + GetDirOffsetY(dirp);
                int ai = chf.cells[ax + ay * chf.width].index + GetCon(s, dirp);
                RcCompactSpan @as = chf.spans[ai];
                ch = Math.Max(ch, @as.y);
                regs[3] = chf.spans[ai].reg | (chf.areas[ai] << 16);
                if (GetCon(@as, dir) != RC_NOT_CONNECTED)
                {
                    int ax2 = ax + GetDirOffsetX(dir);
                    int ay2 = ay + GetDirOffsetY(dir);
                    int ai2 = chf.cells[ax2 + ay2 * chf.width].index + GetCon(@as, dir);
                    RcCompactSpan as2 = chf.spans[ai2];
                    ch = Math.Max(ch, as2.y);
                    regs[2] = chf.spans[ai2].reg | (chf.areas[ai2] << 16);
                }
            }

            // Check if the vertex is special edge vertex, these vertices will be removed later.
            // 检查该顶点是否是特殊边顶点，这些顶点稍后将被删除。   如果满足以下条件之一，则将isBorderVertex设置为true：
            for (int j = 0; j < 4; ++j)
            {
                int a = j;
                int b = (j + 1) & 0x3;
                int c = (j + 2) & 0x3;
                int d = (j + 3) & 0x3;

                // The vertex is a border vertex there are two same exterior cells in a row,
                // followed by two interior cells and none of the regions are out of bounds.
                // 该顶点是边界顶点，连续有两个相同的外部单元格，    有两个相同的外部单元格在一行，后面是两个内部单元格，且没有区域越界。  
                // 后面是两个内部单元格，并且没有任何区域超出范围。     两个外部单元格的区域相同，两个内部单元格的区域也相同。
                bool twoSameExts = (regs[a] & regs[b] & RC_BORDER_REG) != 0 && regs[a] == regs[b];
                bool twoInts = ((regs[c] | regs[d]) & RC_BORDER_REG) == 0;
                bool intsSameArea = (regs[c] >> 16) == (regs[d] >> 16);
                bool noZeros = regs[a] != 0 && regs[b] != 0 && regs[c] != 0 && regs[d] != 0;
                if (twoSameExts && twoInts && intsSameArea && noZeros)
                {
                    isBorderVertex = true;
                    break;
                }
            }

            return ch;
        }

        // 遍历轮廓并生成原始点。它首先选择一个未连接的边缘，然后沿着边缘顺序遍历轮廓，直到回到起点。
        // 这个方法主要用于遍历轮廓并生成原始点，为后续的轮廓生成和简化提供数据支持。
        /*
         *  int x, int y：二维坐标，表示当前处理的点的位置。
            int i：当前处理点在压缩高度场chf中的索引。
            RcCompactHeightfield chf：一个压缩高度场对象，包含地形的高度信息和连接信息。
            int[] flags：整数数组，表示每个点的访问标记。
            List<int> points：用于存储生成的原始点的列表。
         */
        private static void WalkContour(int x, int y, int i, RcCompactHeightfield chf, int[] flags, List<int> points)
        {
            // Choose the first non-connected edge
            // 选择第一个未连接的边缘，即flags[i]中第一个为0的位
            int dir = 0;
            while ((flags[i] & (1 << dir)) == 0)
                dir++;

            // 初始化startDir和starti为当前方向和索引
            int startDir = dir;
            int starti = i;

            int area = chf.areas[i];

            // 遍历轮廓，直到回到起点或达到最大迭代次数（这里设置为40000）
            int iter = 0;
            while (++iter < 40000)
            {
                // 如果当前方向的边缘已访问过，选择边缘角点，计算其高度、区域边界和特殊边缘顶点等信息，并将其添加到points列表中。然后更新访问标记，将当前方向顺时针旋转。
                if ((flags[i] & (1 << dir)) != 0)
                {
                    // Choose the edge corner  选择边角
                    bool isBorderVertex = false;
                    bool isAreaBorder = false;
                    int px = x;
                    int py = GetCornerHeight(x, y, i, dir, chf, out isBorderVertex);
                    int pz = y;
                    switch (dir)
                    {
                        case 0:
                            pz++;
                            break;
                        case 1:
                            px++;
                            pz++;
                            break;
                        case 2:
                            px++;
                            break;
                    }

                    int r = 0;
                    RcCompactSpan s = chf.spans[i];
                    if (GetCon(s, dir) != RC_NOT_CONNECTED)
                    {
                        int ax = x + GetDirOffsetX(dir);
                        int ay = y + GetDirOffsetY(dir);
                        int ai = chf.cells[ax + ay * chf.width].index + GetCon(s, dir);
                        r = chf.spans[ai].reg;
                        if (area != chf.areas[ai])
                            isAreaBorder = true;
                    }

                    if (isBorderVertex)
                        r |= RC_BORDER_VERTEX;
                    if (isAreaBorder)
                        r |= RC_AREA_BORDER;
                    points.Add(px);
                    points.Add(py);
                    points.Add(pz);
                    points.Add(r);

                    flags[i] &= ~(1 << dir); // Remove visited edges   删除访问过的边
                    dir = (dir + 1) & 0x3; // Rotate CW        顺时针旋转    
                }
                else
                {
                    // 如果当前方向的边缘未访问过，计算下一个点的索引和坐标，将当前方向逆时针旋转。
                    
                    int ni = -1;
                    int nx = x + GetDirOffsetX(dir);
                    int ny = y + GetDirOffsetY(dir);
                    RcCompactSpan s = chf.spans[i];
                    if (GetCon(s, dir) != RC_NOT_CONNECTED)
                    {
                        RcCompactCell nc = chf.cells[nx + ny * chf.width];
                        ni = nc.index + GetCon(s, dir);
                    }

                    if (ni == -1)
                    {
                        // Should not happen.     不应该发生。
                        return;
                    }

                    x = nx;
                    y = ny;
                    i = ni;
                    dir = (dir + 3) & 0x3; // Rotate CCW        逆时针旋转
                }

                // 当遍历回到起点时，跳出循环。
                if (starti == i && startDir == dir)
                {
                    break;
                }
            }
        }

        // 计算点到线段的距离，用于后续轮廓简化过程中判断点是否在误差范围内。
        /*
         *  int x, int z：二维坐标，表示待计算距离的点的位置。
            int px, int pz：二维坐标，表示线段的起点位置。
            int qx, int qz：二维坐标，表示线段的终点位置。
         */
        private static float DistancePtSeg(int x, int z, int px, int pz, int qx, int qz)
        {
            // 计算线段PQ的向量pqx和pqz，以及点X到线段起点P的向量dx和dz
            float pqx = qx - px;
            float pqz = qz - pz;
            float dx = x - px;
            float dz = z - pz;
            // 计算点X在线段PQ上的投影比例t。如果线段长度大于0，则t等于点X到线段起点P的向量与线段PQ向量的点积除以线段长度的平方。如果t小于0，则将其设置为0；如果t大于1，则将其设置为1
            float d = pqx * pqx + pqz * pqz;
            float t = pqx * dx + pqz * dz;
            if (d > 0)
                t /= d;
            if (t < 0)
                t = 0;
            else if (t > 1)
                t = 1;

            // 根据投影比例t计算点X在线段PQ上的投影点Y的坐标，然后计算点X到投影点Y的距离。
            dx = px + t * pqx - x;
            dz = pz + t * pqz - z;

            // 返回点X到线段PQ的距离
            return dx * dx + dz * dz;
        }

        // 简化轮廓。首先添加初始点，然后根据最大误差和最大边长添加新点，直到所有原始点都在误差范围内。简化过程中还会处理连接到其他区域的边缘顶点。
        // 这个方法主要用于简化轮廓，可以用于导航网格生成、地形分析等场景，通过控制最大误差和最大边长参数，可以平衡轮廓的简化程度和细节保留。
        /*
         *  List<int> points：表示原始轮廓点的列表。
            List<int> simplified：表示简化后的轮廓点的列表。
            float maxError：允许的最大误差，用于控制轮廓点的简化程度。
            int maxEdgeLen：允许的最大边长，用于控制轮廓边的细分程度。
            int buildFlags：控制轮廓生成和简化过程的标志。
         */
        private static void SimplifyContour(List<int> points, List<int> simplified, float maxError, int maxEdgeLen, int buildFlags)
        {
            // Add initial points.
            // 添加初始点。如果原始轮廓有连接到其他区域的边缘，为每个区域变化的位置添加一个新点。如果没有连接，找到轮廓的左下角和右上角顶点，将它们作为初始点。
            bool hasConnections = false;
            for (int i = 0; i < points.Count; i += 4)
            {
                if ((points[i + 3] & RC_CONTOUR_REG_MASK) != 0)
                {
                    hasConnections = true;
                    break;
                }
            }

            if (hasConnections)
            {
                // The contour has some portals to other regions.
                // Add a new point to every location where the region changes.
                // 等高线有一些通往其他区域的入口。
                // 在区域发生变化的每个位置添加一个新点。
                for (int i = 0, ni = points.Count / 4; i < ni; ++i)
                {
                    int ii = (i + 1) % ni;
                    bool differentRegs = (points[i * 4 + 3] & RC_CONTOUR_REG_MASK) != (points[ii * 4 + 3] & RC_CONTOUR_REG_MASK);
                    bool areaBorders = (points[i * 4 + 3] & RC_AREA_BORDER) != (points[ii * 4 + 3] & RC_AREA_BORDER);
                    if (differentRegs || areaBorders)
                    {
                        simplified.Add(points[i * 4 + 0]);
                        simplified.Add(points[i * 4 + 1]);
                        simplified.Add(points[i * 4 + 2]);
                        simplified.Add(i);
                    }
                }
            }

            if (simplified.Count == 0)
            {
                // If there is no connections at all,
                // create some initial points for the simplification process.
                // Find lower-left and upper-right vertices of the contour.
                // 如果根本没有连接，
                // 为简化过程创建一些初始点。
                // 找到轮廓的左下角和右上角顶点。
                int llx = points[0];
                int lly = points[1];
                int llz = points[2];
                int lli = 0;
                int urx = points[0];
                int ury = points[1];
                int urz = points[2];
                int uri = 0;
                for (int i = 0; i < points.Count; i += 4)
                {
                    int x = points[i + 0];
                    int y = points[i + 1];
                    int z = points[i + 2];
                    if (x < llx || (x == llx && z < llz))
                    {
                        llx = x;
                        lly = y;
                        llz = z;
                        lli = i / 4;
                    }

                    if (x > urx || (x == urx && z > urz))
                    {
                        urx = x;
                        ury = y;
                        urz = z;
                        uri = i / 4;
                    }
                }

                simplified.Add(llx);
                simplified.Add(lly);
                simplified.Add(llz);
                simplified.Add(lli);

                simplified.Add(urx);
                simplified.Add(ury);
                simplified.Add(urz);
                simplified.Add(uri);
            }

            // 添加新点，直到所有原始点都在误差范围内。对于每个简化轮廓上的边，找到距离该边最远的原始点，如果该点到边的距离大于最大误差，将其添加到简化轮廓中。
            // Add points until all raw points are within error tolerance to the simplified shape.
            // 添加点，直到所有原始点都在简化形状的误差容限内。
            int pn = points.Count / 4;
            for (int i = 0; i < simplified.Count / 4;)
            {
                int ii = (i + 1) % (simplified.Count / 4);

                int ax = simplified[i * 4 + 0];
                int az = simplified[i * 4 + 2];
                int ai = simplified[i * 4 + 3];

                int bx = simplified[ii * 4 + 0];
                int bz = simplified[ii * 4 + 2];
                int bi = simplified[ii * 4 + 3];

                // Find maximum deviation from the segment.
                // 找到与线段的最大偏差。
                float maxd = 0;
                int maxi = -1;
                int ci, cinc, endi;

                // Traverse the segment in lexilogical order so that the max deviation is calculated similarly when traversing opposite segments.
                // 按照词法顺序遍历线段，这样在遍历相反的线段时，最大偏差的计算方式类似。
                if (bx > ax || (bx == ax && bz > az))
                {
                    cinc = 1;
                    ci = (ai + cinc) % pn;
                    endi = bi;
                }
                else
                {
                    cinc = pn - 1;
                    ci = (bi + cinc) % pn;
                    endi = ai;
                    int temp = ax;
                    ax = bx;
                    bx = temp;
                    temp = az;
                    az = bz;
                    bz = temp;
                }

                // Tessellate only outer edges or edges between areas.
                // 仅对外部边缘或区域之间的边缘进行细分。
                if ((points[ci * 4 + 3] & RC_CONTOUR_REG_MASK) == 0 || (points[ci * 4 + 3] & RC_AREA_BORDER) != 0)
                {
                    while (ci != endi)
                    {
                        float d = DistancePtSeg(points[ci * 4 + 0], points[ci * 4 + 2], ax, az, bx, bz);
                        if (d > maxd)
                        {
                            maxd = d;
                            maxi = ci;
                        }

                        ci = (ci + cinc) % pn;
                    }
                }

                // If the max deviation is larger than accepted error,
                // add new point, else continue to next segment.
                // 如果最大偏差大于可接受的误差，
                // 添加新点，否则继续下一段。
                if (maxi != -1 && maxd > (maxError * maxError))
                {
                    // Add the point.    添加要点。
                    simplified.Insert((i + 1) * 4 + 0, points[maxi * 4 + 0]);
                    simplified.Insert((i + 1) * 4 + 1, points[maxi * 4 + 1]);
                    simplified.Insert((i + 1) * 4 + 2, points[maxi * 4 + 2]);
                    simplified.Insert((i + 1) * 4 + 3, maxi);
                }
                else
                {
                    ++i;
                }
            }

            // 对于过长的边进行细分。如果设置了细分标志（如墙壁边缘或区域边缘），并且边长大于最大边长，将边的中点添加到简化轮廓中。
            // Split too long edges.  分割太长的边缘。
            if (maxEdgeLen > 0 && (buildFlags & (RcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES | RcBuildContoursFlags.RC_CONTOUR_TESS_AREA_EDGES)) != 0)
            {
                for (int i = 0; i < simplified.Count / 4;)
                {
                    int ii = (i + 1) % (simplified.Count / 4);

                    int ax = simplified[i * 4 + 0];
                    int az = simplified[i * 4 + 2];
                    int ai = simplified[i * 4 + 3];

                    int bx = simplified[ii * 4 + 0];
                    int bz = simplified[ii * 4 + 2];
                    int bi = simplified[ii * 4 + 3];

                    // Find maximum deviation from the segment.  找出与线段的最大偏差。
                    int maxi = -1;
                    int ci = (ai + 1) % pn;

                    // Tessellate only outer edges or edges between areas.   仅对外部边缘或区域之间的边缘进行镶嵌。
                    bool tess = false;
                    // Wall edges.  墙边
                    if ((buildFlags & RcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES) != 0 && (points[ci * 4 + 3] & RC_CONTOUR_REG_MASK) == 0)
                    {
                        tess = true;
                    }

                    // Edges between areas.  区域之间的边缘。
                    if ((buildFlags & RcBuildContoursFlags.RC_CONTOUR_TESS_AREA_EDGES) != 0 && (points[ci * 4 + 3] & RC_AREA_BORDER) != 0)
                    {
                        tess = true;
                    }

                    if (tess)
                    {
                        int dx = bx - ax;
                        int dz = bz - az;
                        if (dx * dx + dz * dz > maxEdgeLen * maxEdgeLen)
                        {
                            // Round based on the segments in lexilogical order so that the max tesselation is consistent regardless in which direction segments are traversed.
                            // 基于字典顺序的段进行舍入，以便无论段在哪个方向遍历，最大镶嵌都是一致的。
                            int n = bi < ai ? (bi + pn - ai) : (bi - ai);
                            if (n > 1)
                            {
                                if (bx > ax || (bx == ax && bz > az))
                                    maxi = (ai + n / 2) % pn;
                                else
                                    maxi = (ai + (n + 1) / 2) % pn;
                            }
                        }
                    }

                    // If the max deviation is larger than accepted error,
                    // add new point, else continue to next segment.
                    // 如果最大偏差大于可接受的误差，
                    // 添加新点，否则继续下一段。
                    if (maxi != -1)
                    {
                        // Add the point.    Add the point.
                        simplified.Insert((i + 1) * 4 + 0, points[maxi * 4 + 0]);
                        simplified.Insert((i + 1) * 4 + 1, points[maxi * 4 + 1]);
                        simplified.Insert((i + 1) * 4 + 2, points[maxi * 4 + 2]);
                        simplified.Insert((i + 1) * 4 + 3, maxi);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            // 更新简化轮廓点的边缘顶点标志和相邻区域信息。
            for (int i = 0; i < simplified.Count / 4; ++i)
            {
                // The edge vertex flag is take from the current raw point,
                // and the neighbour region is take from the next raw point.
                // 边顶点标志取自当前原始点，
                // 相邻区域取自下一个原始点。
                int ai = (simplified[i * 4 + 3] + 1) % pn;
                int bi = simplified[i * 4 + 3];
                simplified[i * 4 + 3] = (points[ai * 4 + 3] & (RC_CONTOUR_REG_MASK | RC_AREA_BORDER))
                                        | points[bi * 4 + 3] & RC_BORDER_VERTEX;
            }
        }

        // 计算多边形的面积。
        // 这个方法主要用于计算二维多边形的面积，可以用于判断多边形的大小、合并相邻多边形等操作。注意，这个方法仅适用于凸多边形，对于凹多边形或自相交多边形，计算结果可能不正确。
        /*
         *  int[] verts：表示多边形顶点的整数数组，每个顶点包含4个整数（x, y, z, r），但只使用x和z坐标进行计算。
            int nverts：多边形的顶点数量。
         */
        private static int CalcAreaOfPolygon2D(int[] verts, int nverts)
        {
            int area = 0;
            // 遍历多边形的所有顶点，计算相邻顶点组成的三角形的有向面积，累加到area中。
            for (int i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                int vi = i * 4;
                int vj = j * 4;
                // 有向面积的计算公式为
                area += verts[vi + 0] * verts[vj + 2] - verts[vj + 0] * verts[vi + 2];
            }

            // 由于有向面积可能为负值，将累加后的area加1，然后除以2，得到最终的多边形面积。
            return (area + 1) / 2;
        }

        // 检查线段是否与轮廓相交。对于每条边，如果线段与边相交，则返回true，否则返回false。
        /*
         *  int d0, int d1：表示待检查线段的两个顶点在d0verts和d1verts中的索引。
            int i：表示当前处理的轮廓点在verts中的索引。
            int n：轮廓的顶点数量。
            int[] verts：表示轮廓顶点的整数数组。
            int[] d0verts, int[] d1verts：表示待检查线段顶点的整数数组。
         */
        private static bool IntersectSegContour(int d0, int d1, int i, int n, int[] verts, int[] d0verts, int[] d1verts)
        {
            // 初始化一个长度为16的整数数组pverts，用于存储待检查线段的顶点和轮廓边的顶点。
            // For each edge (k,k+1) of P
            int[] pverts = new int[4 * 4];
            for (int g = 0; g < 4; g++)
            {
                pverts[g] = d0verts[d0 + g];
                pverts[4 + g] = d1verts[d1 + g];
            }

            
            // 遍历轮廓的所有边，跳过与当前处理点相邻的边。
            d0 = 0;
            d1 = 4;
            for (int k = 0; k < n; k++)
            {
                int k1 = RcMeshs.Next(k, n);
                // Skip edges incident to i.
                if (i == k || i == k1)
                    continue;
                
                // 将待检查线段的顶点和当前轮廓边的顶点分别存储在pverts的前8个和后8个元素中。
                int p0 = k * 4;
                int p1 = k1 * 4;
                for (int g = 0; g < 4; g++)
                {
                    pverts[8 + g] = verts[p0 + g];
                    pverts[12 + g] = verts[p1 + g];
                }

                p0 = 8;
                p1 = 12;
                // 如果待检查线段的顶点与当前轮廓边的顶点相同，跳过当前轮廓边。
                if (RcMeshs.VEqual(pverts, d0, p0) || RcMeshs.VEqual(pverts, d1, p0) ||
                    RcMeshs.VEqual(pverts, d0, p1) || RcMeshs.VEqual(pverts, d1, p1))
                    continue;

                // 判断待检查线段是否与当前轮廓边相交，如果相交，返回true。
                if (RcMeshs.Intersect(pverts, d0, d1, p0, p1))
                    return true;
            }

            // 遍历完所有轮廓边，如果没有相交的边，返回false。
            return false;
        }

        // 判断一个点是否在一个凸多边形的某个顶点的锥形区域内。这个方法在合并轮廓时用于寻找合适的连接顶点。
        // 这个方法主要用于合并轮廓时寻找合适的连接顶点，可以确保连接的顶点在凸多边形的锥形区域内，从而避免生成自相交的轮廓。
        /*
         *  int i：表示凸多边形顶点在verts中的索引。
            int n：凸多边形的顶点数量。
            int[] verts：表示凸多边形顶点的整数数组。
            int pj：表示待判断点在vertpj中的索引。
            int[] vertpj：表示待判断点的整数数组。
         */
        private static bool InCone(int i, int n, int[] verts, int pj, int[] vertpj)
        {
            // 初始化一个长度为16的整数数组pverts，用于存储凸多边形的相邻顶点和待判断点
            int pi = i * 4;
            int pi1 = RcMeshs.Next(i, n) * 4;
            int pin1 = RcMeshs.Prev(i, n) * 4;
            int[] pverts = new int[4 * 4];
            for (int g = 0; g < 4; g++)
            {
                pverts[g] = verts[pi + g];
                pverts[4 + g] = verts[pi1 + g];
                pverts[8 + g] = verts[pin1 + g];
                pverts[12 + g] = vertpj[pj + g];
            }

            pi = 0;
            pi1 = 4;
            pin1 = 8;
            pj = 12;
            // 判断凸多边形的顶点i是否是凸顶点（即顶点i+1在顶点i-1和顶点i的连线的左侧或者在连线上）。如果是凸顶点，则判断待判断点是否在以顶点i为顶点的锥形区域内。
            // If P[i] is a convex vertex [ i+1 left or on (i-1,i) ].      如果 P[i] 是凸顶点 [ i+1 left 或 on (i-1,i) ]。
            if (RcMeshs.LeftOn(pverts, pin1, pi, pi1))
                return RcMeshs.Left(pverts, pi, pj, pin1) && RcMeshs.Left(pverts, pj, pi, pi1);
            
            // 如果顶点i不是凸顶点（即为凹顶点），则判断待判断点是否不在以顶点i为顶点的锥形区域内。
            // Assume (i-1,i,i+1) not collinear.       假设 (i-1,i,i+1) 不共线。
            // else P[i] is reflex.         否则 P[i] 是反射。
            return !(RcMeshs.LeftOn(pverts, pi, pj, pi1) && RcMeshs.LeftOn(pverts, pj, pi, pin1));
        }

        // 移除轮廓中的退化线段，即相邻顶点在xz平面上相等的线段。这个方法在简化轮廓后用于清理无效的线段。
        // List<int> simplified：表示简化后的轮廓顶点列表。
        private static void RemoveDegenerateSegments(List<int> simplified)
        {
            // Remove adjacent vertices which are equal on xz-plane,  删除 xz 平面上相等的相邻顶点，
            // or else the triangulator will get confused.       否则三角仪会感到困惑。
            
            // 遍历简化后的轮廓顶点列表，计算每个顶点的下一个顶点（ni）
            int npts = simplified.Count / 4;
            for (int i = 0; i < npts; ++i)
            {
                int ni = RcMeshs.Next(i, npts);

                // 比较当前顶点（i）和下一个顶点（ni）在xz平面上是否相等。如果相等，说明这是一个退化线段。
                // if (Vequal(&simplified[i*4], &simplified[ni*4]))
                if (simplified[i * 4] == simplified[ni * 4]
                    && simplified[i * 4 + 2] == simplified[ni * 4 + 2])
                {
                    // Degenerate segment, remove.
                    // 移除退化线段，即从列表中删除当前顶点
                    simplified.RemoveAt(i * 4);
                    simplified.RemoveAt(i * 4);
                    simplified.RemoveAt(i * 4);
                    simplified.RemoveAt(i * 4);
                    npts--;
                }
            }
        }

        // 合并两个轮廓，用于将一个区域的轮廓和一个孔洞的轮廓连接起来。
        // 用于合并两个轮廓。这个方法主要用于将一个区域的轮廓和一个孔洞的轮廓连接起来。
        // 这个方法在合并轮廓时可以保证合并后的轮廓顶点顺序正确，从而避免生成自相交的轮廓。
        /*
         *  RcContour ca, RcContour cb：表示待合并的两个轮廓。
            int ia, int ib：表示合并点在轮廓A和轮廓B中的索引。
         */
        private static void MergeContours(RcContour ca, RcContour cb, int ia, int ib)
        {
            // 计算合并后的轮廓的最大顶点数量，即轮廓A和轮廓B的顶点数量之和加2
            int maxVerts = ca.nverts + cb.nverts + 2;
            // 初始化一个长度为最大顶点数量乘以4的整数数组verts，用于存储合并后的轮廓顶点
            int[] verts = new int[maxVerts * 4];

            int nv = 0;

            // Copy contour A.
            // 遍历轮廓A的顶点，从合并点开始，将顶点依次复制到verts中
            for (int i = 0; i <= ca.nverts; ++i)
            {
                int dst = nv * 4;
                int src = ((ia + i) % ca.nverts) * 4;
                verts[dst + 0] = ca.verts[src + 0];
                verts[dst + 1] = ca.verts[src + 1];
                verts[dst + 2] = ca.verts[src + 2];
                verts[dst + 3] = ca.verts[src + 3];
                nv++;
            }

            // Copy contour B
            // 遍历轮廓B的顶点，从合并点开始，将顶点依次复制到verts中
            for (int i = 0; i <= cb.nverts; ++i)
            {
                int dst = nv * 4;
                int src = ((ib + i) % cb.nverts) * 4;
                verts[dst + 0] = cb.verts[src + 0];
                verts[dst + 1] = cb.verts[src + 1];
                verts[dst + 2] = cb.verts[src + 2];
                verts[dst + 3] = cb.verts[src + 3];
                nv++;
            }

            // 更新轮廓A的顶点数组和顶点数量，将轮廓B的顶点数组设置为null，顶点数量设置为0
            ca.verts = verts;
            ca.nverts = nv;

            cb.verts = null;
            cb.nverts = 0;
        }

        // Finds the lowest leftmost vertex of a contour.
        // 找到一个轮廓的最左边的顶点，用于合并孔洞时确定合适的连接点。
        // RcContour contour：表示待查找最左顶点的轮廓。
        private static int[] FindLeftMostVertex(RcContour contour)
        {
            // 初始化最左顶点的x坐标（minx）、z坐标（minz）和索引（leftmost）为轮廓的第一个顶点
            int minx = contour.verts[0];
            int minz = contour.verts[2];
            int leftmost = 0;
            // 遍历轮廓的顶点，如果当前顶点的x坐标小于minx，或者x坐标等于minx且z坐标小于minz，则更新最左顶点的坐标和索引
            for (int i = 1; i < contour.nverts; i++)
            {
                int x = contour.verts[i * 4 + 0];
                int z = contour.verts[i * 4 + 2];
                if (x < minx || (x == minx && z < minz))
                {
                    minx = x;
                    minz = z;
                    leftmost = i;
                }
            }

            // 返回一个包含最左顶点x坐标、z坐标和索引的整数数组
            return new int[] { minx, minz, leftmost };
        }

        // 合并一个区域的所有孔洞。这个方法首先对孔洞按照最左顶点的位置进行排序，然后依次将孔洞与区域的轮廓合并。
        // 这个方法在导航网格生成过程中的轮廓构建阶段用于合并区域内的孔洞，可以确保合并后的轮廓不自相交，便于后续的三角化和寻路操作。
        /*
         *  RcTelemetry ctx：表示导航网格生成过程中的上下文，用于记录警告信息。
            RcContourRegion region：表示待合并孔洞的区域。
         */
        private static void MergeRegionHoles(RcTelemetry ctx, RcContourRegion region)
        {
            // Sort holes from left to right.
            // 对孔洞按照最左顶点的位置进行排序
            for (int i = 0; i < region.nholes; i++)
            {
                int[] minleft = FindLeftMostVertex(region.holes[i].contour);
                region.holes[i].minx = minleft[0];
                region.holes[i].minz = minleft[1];
                region.holes[i].leftmost = minleft[2];
            }

            Array.Sort(region.holes, RcContourHoleComparer.Shared);

            // 计算合并后的轮廓的最大顶点数量，即区域轮廓和所有孔洞轮廓的顶点数量之和
            int maxVerts = region.outline.nverts;
            for (int i = 0; i < region.nholes; i++)
                maxVerts += region.holes[i].contour.nverts;

            // 初始化一个RcPotentialDiagonal数组，用于存储潜在的对角线
            RcPotentialDiagonal[] diags = new RcPotentialDiagonal[maxVerts];
            for (int pd = 0; pd < maxVerts; pd++)
            {
                diags[pd] = new RcPotentialDiagonal();
            }

            RcContour outline = region.outline;

            // 遍历所有孔洞，将孔洞依次合并到区域轮廓中。对于每个孔洞，执行以下操作
            // Merge holes into the outline one by one.
            for (int i = 0; i < region.nholes; i++)
            {
                RcContour hole = region.holes[i].contour;

                int index = -1;
                int bestVertex = region.holes[i].leftmost;
                for (int iter = 0; iter < hole.nverts; iter++)
                {
                    //  找到潜在的对角线，即满足锥形区域条件的区域轮廓顶点
                    // Find potential diagonals.
                    // The 'best' vertex must be in the cone described by 3 consecutive vertices of the outline.
                    // ..o j-1
                    // |
                    // | * best
                    // |
                    // j o-----o j+1
                    // :
                    int ndiags = 0;
                    int corner = bestVertex * 4;
                    for (int j = 0; j < outline.nverts; j++)
                    {
                        if (InCone(j, outline.nverts, outline.verts, corner, hole.verts))
                        {
                            int dx = outline.verts[j * 4 + 0] - hole.verts[corner + 0];
                            int dz = outline.verts[j * 4 + 2] - hole.verts[corner + 2];
                            diags[ndiags].vert = j;
                            diags[ndiags].dist = dx * dx + dz * dz;
                            ndiags++;
                        }
                    }

                    // 按照距离对潜在的对角线进行排序，我们希望连接尽可能短的对角线
                    // Sort potential diagonals by distance, we want to make the connection as short as possible.
                    Array.Sort(diags, 0, ndiags, RcPotentialDiagonalComparer.Shared);

                    // 查找不与区域轮廓和剩余孔洞相交的对角线
                    // Find a diagonal that is not intersecting the outline not the remaining holes.
                    index = -1;
                    for (int j = 0; j < ndiags; j++)
                    {
                        int pt = diags[j].vert * 4;
                        bool intersect = IntersectSegContour(pt, corner, diags[j].vert, outline.nverts, outline.verts,
                            outline.verts, hole.verts);
                        for (int k = i; k < region.nholes && !intersect; k++)
                            intersect |= IntersectSegContour(pt, corner, -1, region.holes[k].contour.nverts,
                                region.holes[k].contour.verts, outline.verts, hole.verts);
                        if (!intersect)
                        {
                            index = diags[j].vert;
                            break;
                        }
                    }

                    // 如果找到了不相交的对角线，将孔洞与区域轮廓合并
                    // If found non-intersecting diagonal, stop looking.
                    if (index != -1)
                        break;
                    // All the potential diagonals for the current vertex were intersecting, try next vertex.
                    bestVertex = (bestVertex + 1) % hole.nverts;
                }

                if (index == -1)
                {
                    ctx.Warn("mergeHoles: Failed to find merge points for");
                    continue;
                }

                MergeContours(region.outline, hole, index, bestVertex);
            }
        }

        /// @par
        /// 构建导航网格的轮廓。这个方法首先根据输入的高度场数据计算原始轮廓，然后根据给定的最大误差和最大边长参数对轮廓进行简化。最后，合并轮廓中的孔洞。
        /// 
        /// The raw contours will match the region outlines exactly. The @p maxError and @p maxEdgeLen
        /// parameters control how closely the simplified contours will match the raw contours.
        /// 原始轮廓将与区域轮廓完全匹配。 @p maxError 和 @p maxEdgeLen 参数控制简化轮廓与原始轮廓的匹配程度。
        ///
        /// Simplified contours are generated such that the vertices for portals between areas match up.
        /// (They are considered mandatory vertices.)
        /// 生成简化的轮廓，以便区域之间的门户顶点匹配。
        /// （它们被视为强制顶点。）
        ///
        /// Setting @p maxEdgeLength to zero will disabled the edge length feature.  将@p maxEdgeLength 设置为零将禁用边缘长度功能。
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.  有关配置参数的更多信息，请参阅#rcConfig 文档。
        ///
        /// @see rcAllocContourSet, rcCompactHeightfield, rcContourSet, rcConfig
        /*
         *  RcTelemetry ctx：表示导航网格生成过程中的上下文，用于记录警告信息。
            RcCompactHeightfield chf：表示输入的高度场数据。
            float maxError：表示简化轮廓时允许的最大误差。
            int maxEdgeLen：表示简化轮廓时允许的最大边长。
            int buildFlags：表示轮廓构建的标志，用于控制轮廓生成的选项。
         */
        public static RcContourSet BuildContours(RcTelemetry ctx, RcCompactHeightfield chf, float maxError, int maxEdgeLen,
            int buildFlags)
        {
            // 初始化一个RcContourSet对象，用于存储生成的轮廓
            int w = chf.width;
            int h = chf.height;
            int borderSize = chf.borderSize;
            RcContourSet cset = new RcContourSet();

            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_BUILD_CONTOURS);

            // 标记轮廓的边界
            cset.bmin = chf.bmin;
            cset.bmax = chf.bmax;
            if (borderSize > 0)
            {
                // If the heightfield was build with bordersize, remove the offset.  如果 heightfield 是使用 bordersize 构建的，请删除偏移量。
                float pad = borderSize * chf.cs;
                cset.bmin.x += pad;
                cset.bmin.z += pad;
                cset.bmax.x -= pad;
                cset.bmax.z -= pad;
            }

            cset.cs = chf.cs;
            cset.ch = chf.ch;
            cset.width = chf.width - chf.borderSize * 2;
            cset.height = chf.height - chf.borderSize * 2;
            cset.borderSize = chf.borderSize;
            cset.maxError = maxError;

            int[] flags = new int[chf.spanCount];

            ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_CONTOURS_TRACE);

            // Mark boundaries. 标记边界。    
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        int res = 0;
                        RcCompactSpan s = chf.spans[i];
                        if (chf.spans[i].reg == 0 || (chf.spans[i].reg & RC_BORDER_REG) != 0)
                        {
                            flags[i] = 0;
                            continue;
                        }

                        for (int dir = 0; dir < 4; ++dir)
                        {
                            int r = 0;
                            if (GetCon(s, dir) != RC_NOT_CONNECTED)
                            {
                                int ax = x + GetDirOffsetX(dir);
                                int ay = y + GetDirOffsetY(dir);
                                int ai = chf.cells[ax + ay * w].index + GetCon(s, dir);
                                r = chf.spans[ai].reg;
                            }

                            if (r == chf.spans[i].reg)
                                res |= (1 << dir);
                        }

                        flags[i] = res ^ 0xf; // Inverse, mark non connected edges.  // 反之，标记非连接边。
                    }
                }
            }

            ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_CONTOURS_TRACE);

            List<int> verts = new List<int>(256);
            List<int> simplified = new List<int>(64);

            // 遍历高度场的单元格，对每个单元格执行以下操作：
            /*a. 提取轮廓顶点。
            b. 简化轮廓。
            c. 移除退化线段。
            d. 存储轮廓。
            */
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        if (flags[i] == 0 || flags[i] == 0xf)
                        {
                            flags[i] = 0;
                            continue;
                        }

                        int reg = chf.spans[i].reg;
                        if (reg == 0 || (reg & RC_BORDER_REG) != 0)
                            continue;
                        int area = chf.areas[i];

                        verts.Clear();
                        simplified.Clear();

                        ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_CONTOURS_WALK);
                        WalkContour(x, y, i, chf, flags, verts);
                        ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_CONTOURS_WALK);

                        ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_CONTOURS_SIMPLIFY);
                        SimplifyContour(verts, simplified, maxError, maxEdgeLen, buildFlags);
                        RemoveDegenerateSegments(simplified);
                        ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_CONTOURS_SIMPLIFY);

                        // Store region->contour remap info.
                        // Create contour.
                        // 存储区域->轮廓重映射信息。
                        // 创建轮廓。
                        if (simplified.Count / 4 >= 3)
                        {
                            RcContour cont = new RcContour();
                            cset.conts.Add(cont);

                            cont.nverts = simplified.Count / 4;
                            cont.verts = new int[simplified.Count];
                            for (int l = 0; l < cont.verts.Length; l++)
                            {
                                cont.verts[l] = simplified[l];
                            }

                            if (borderSize > 0)
                            {
                                // If the heightfield was build with bordersize, remove the offset.
                                // 如果 heightfield 是使用 bordersize 构建的，则删除偏移量。
                                for (int j = 0; j < cont.nverts; ++j)
                                {
                                    cont.verts[j * 4] -= borderSize;
                                    cont.verts[j * 4 + 2] -= borderSize;
                                }
                            }

                            cont.nrverts = verts.Count / 4;
                            cont.rverts = new int[verts.Count];
                            for (int l = 0; l < cont.rverts.Length; l++)
                            {
                                cont.rverts[l] = verts[l];
                            }

                            if (borderSize > 0)
                            {
                                // If the heightfield was build with bordersize, remove the offset.
                                // 如果 heightfield 是使用 bordersize 构建的，则删除偏移量。
                                for (int j = 0; j < cont.nrverts; ++j)
                                {
                                    cont.rverts[j * 4] -= borderSize;
                                    cont.rverts[j * 4 + 2] -= borderSize;
                                }
                            }

                            cont.reg = reg;
                            cont.area = area;
                        }
                    }
                }
            }

            // Merge holes if needed.
            // 如果需要的话合并洞。
            if (cset.conts.Count > 0)
            {
                // Calculate winding of all polygons.
                // 计算所有多边形的缠绕。
                int[] winding = new int[cset.conts.Count];
                int nholes = 0;
                for (int i = 0; i < cset.conts.Count; ++i)
                {
                    RcContour cont = cset.conts[i];
                    // If the contour is wound backwards, it is a hole.
                    // 如果轮廓向后缠绕，则为孔。
                    winding[i] = CalcAreaOfPolygon2D(cont.verts, cont.nverts) < 0 ? -1 : 1;
                    if (winding[i] < 0)
                        nholes++;
                }

                if (nholes > 0)
                {
                    // Collect outline contour and holes contours per region.
                    // We assume that there is one outline and multiple holes.
                    // 收集每个区域的轮廓轮廓和孔轮廓。
                    // 我们假设有一个轮廓和多个孔。
                    int nregions = chf.maxRegions + 1;
                    RcContourRegion[] regions = new RcContourRegion[nregions];
                    for (int i = 0; i < nregions; i++)
                    {
                        regions[i] = new RcContourRegion();
                    }

                    for (int i = 0; i < cset.conts.Count; ++i)
                    {
                        RcContour cont = cset.conts[i];
                        // Positively would contours are outlines, negative holes.
                        // 积极的轮廓是轮廓，消极的则是孔。
                        if (winding[i] > 0)
                        {
                            if (regions[cont.reg].outline != null)
                            {
                                throw new Exception(
                                    "rcBuildContours: Multiple outlines for region " + cont.reg + ".");
                            }

                            regions[cont.reg].outline = cont;
                        }
                        else
                        {
                            regions[cont.reg].nholes++;
                        }
                    }

                    for (int i = 0; i < nregions; i++)
                    {
                        if (regions[i].nholes > 0)
                        {
                            regions[i].holes = new RcContourHole[regions[i].nholes];
                            for (int nh = 0; nh < regions[i].nholes; nh++)
                            {
                                regions[i].holes[nh] = new RcContourHole();
                            }

                            regions[i].nholes = 0;
                        }
                    }

                    for (int i = 0; i < cset.conts.Count; ++i)
                    {
                        RcContour cont = cset.conts[i];
                        RcContourRegion reg = regions[cont.reg];
                        if (winding[i] < 0)
                            reg.holes[reg.nholes++].contour = cont;
                    }

                    // Finally merge each regions holes into the outline.
                    // 最后将每个区域的孔合并到轮廓中。
                    for (int i = 0; i < nregions; i++)
                    {
                        RcContourRegion reg = regions[i];
                        if (reg.nholes == 0)
                            continue;

                        if (reg.outline != null)
                        {
                            MergeRegionHoles(ctx, reg);
                        }
                        else
                        {
                            // The region does not have an outline.
                            // This can happen if the contour becaomes selfoverlapping because of too aggressive simplification settings.
                            // 该区域没有轮廓。
                            // 如果轮廓由于过于激进的简化设置而变得自重叠，则可能会发生这种情况。
                            throw new Exception("rcBuildContours: Bad outline for region " + i
                                                                                           + ", contour simplification is likely too aggressive.");
                        }
                    }
                }
            }

            return cset;
        }
    }
}