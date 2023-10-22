/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
Recast4J Copyright (c) 2015 Piotr Piastucki piotr@jtilia.org
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
    /** Represents a polygon mesh suitable for use in building a navigation mesh. */
 
    // 表示一个适用于构建导航网格的多边形网格。这个类通常用于存储导航网格的基本结构，例如顶点、多边形和相邻信息等。
    // 表示网格中多边形边缘的最大误差。
    public class RcPolyMesh
    {
        /** The mesh vertices. [Form: (x, y, z) coordinates * #nverts] */
        // 表示网格顶点。格式为 (x, y, z) 坐标 * nverts
        public int[] verts;

        /** Polygon and neighbor data. [Length: #maxpolys * 2 * #nvp] */
        // 表示多边形和相邻数据。数组长度为 maxpolys * 2 * nvp。
        public int[] polys;

        /** The region id assigned to each polygon. [Length: #maxpolys] */
        // 表示分配给每个多边形的区域 ID。数组长度为 maxpolys。
        public int[] regs;

        /** The area id assigned to each polygon. [Length: #maxpolys] */
        // 表示分配给每个多边形的区域 ID。数组长度为 maxpolys。
        public int[] areas;

        /** The number of vertices. */
        // 表示顶点数量。
        public int nverts;

        /** The number of polygons. */
        // 表示多边形数量。
        public int npolys;

        /** The maximum number of vertices per polygon. */
        // 表示每个多边形的最大顶点数量。
        public int nvp;

        /** The number of allocated polygons. */
        // 表示分配的多边形数量。
        public int maxpolys;

        /** The user defined flags for each polygon. [Length: #maxpolys] */
        // 表示每个多边形的用户定义标志。数组长度为 maxpolys。
        public int[] flags;

        /** The minimum bounds in world space. [(x, y, z)] */
        //表示每个多边形的用户定义标志。数组长度为 maxpolys。
        public RcVec3f bmin = new RcVec3f();

        /** The maximum bounds in world space. [(x, y, z)] */
        // 表示世界空间中的最大边界。格式为 (x, y, z)。
        public RcVec3f bmax = new RcVec3f();

        /** The size of each cell. (On the xz-plane.) */
        // 表示每个单元格在 xz-平面上的大小。
        public float cs;

        /** The height of each cell. (The minimum increment along the y-axis.) */
        // 表示每个单元格在 y-轴上的高度（最小增量）。
        public float ch;

        /** The AABB border size used to generate the source data from which the mesh was derived. */
        // 表示用于生成源数据的 AABB 边界大小，从源数据中派生出网格。
        public int borderSize;

        /** The max error of the polygon edges in the mesh. */
        // 表示网格中多边形边缘的最大误差。
        public float maxEdgeError;
    }
}