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

namespace DotRecast.Recast
{
    /** Represents a simple, non-overlapping contour in field space. */
    // 表示场景空间中的一个简单、非重叠轮廓。在导航网格构建过程中，轮廓用于表示连通区域的边界。
    // 这个类的主要目的是在导航网格构建过程中表示非重叠轮廓，以便在后续步骤中正确处理连通区域的边界。轮廓通常用于生成导航网格的边界多边形，从而生成最终的导航网格。
    public class RcContour
    {
        /** Simplified contour vertex and connection data. [Size: 4 * #nverts] */
        // 简化轮廓的顶点和连接数据。[大小：4 * #nverts]
        public int[] verts;

        /** The number of vertices in the simplified contour. */
        // 简化轮廓中的顶点数量。
        public int nverts;

        /** Raw contour vertex and connection data. [Size: 4 * #nrverts] */
        // 原始轮廓的顶点和连接数据。[大小：4 * #nrverts]
        public int[] rverts;

        /** The number of vertices in the raw contour. */
        // 原始轮廓中的顶点数量
        public int nrverts;

        /** The region id of the contour. */
        // 轮廓的区域ID
        public int area;

        /** The area id of the contour. */
        // 轮廓的区域ID
        public int reg;
    }
}