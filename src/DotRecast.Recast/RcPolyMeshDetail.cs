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
    /**
 * Contains triangle meshes that represent detailed height data associated with the polygons in its associated polygon mesh object.
     *包含三角形网格，表示与其关联的多边形网格对象中的多边形关联的详细高度数据。
     * 
     * 表示与关联的多边形网格对象中的多边形相关的详细高度数据的三角形网格。这个类通常用于存储导航网格的细节信息，例如地形的高度变化等。
     * 类提供了一个用于存储导航网格细节信息的数据结构。这些细节信息可以用于更精确地处理地形高度变化等问题，从而提高寻路和避障等功能的准确性。
 */
    public class RcPolyMeshDetail
    {
        /** The sub-mesh data. [Size: 4*#nmeshes] */
        // 表示子网格数据。数组的大小为 4 * nmeshes。子网格用于将详细的三角形网格划分为更小的部分，以便于处理和优化。
        public int[] meshes;

        /** The mesh vertices. [Size: 3*#nverts] */
        // 表示网格顶点。数组的大小为 3 * nverts。顶点用于定义三角形网格的结构。
        public float[] verts;

        /** The mesh triangles. [Size: 4*#ntris] */
        // 表示网格三角形。数组的大小为 4 * ntris。三角形用于表示详细高度数据的表面。
        public int[] tris;

        /** The number of sub-meshes defined by #meshes. */
        // 表示由 meshes 定义的子网格数量。这个值用于确定子网格数据的大小和范围。
        public int nmeshes;

        /** The number of vertices in #verts. */
        // 表示 verts 中的顶点数量。这个值用于确定顶点数据的大小和范围。
        public int nverts;

        /** The number of triangles in #tris. */
        // 表示 tris 中的三角形数量。这个值用于确定三角形数据的大小和范围。
        public int ntris;
    }
}