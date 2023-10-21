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

using System.Collections.Generic;

namespace DotRecast.Recast.Geom
{
    /// <summary>
    /// 表示一个三角形网格（Triangle Mesh），它包含了一个三角形网格的顶点和面信息，
    /// 以及一个分块三角形网格（Chunky Triangle Mesh）实例，用于加速空间查询。
    /// </summary>
    public class RcTriMesh
    {
        /// <summary>
        /// 表示三角形网格的顶点坐标，是一个浮点数数组。数组中每三个元素表示一个顶点的x、y、z坐标。
        /// </summary>
        private readonly float[] vertices;
        /// <summary>
        /// 表示三角形网格的面信息，是一个整数数组。数组中每三个元素表示一个三角形面的三个顶点索引。
        /// </summary>
        private readonly int[] faces;
        /// <summary>
        /// 表示一个分块三角形网格实例，用于加速对三角形网格的空间查询。
        /// </summary>
        public readonly RcChunkyTriMesh chunkyTriMesh;

        public RcTriMesh(float[] vertices, int[] faces)
        {
            this.vertices = vertices;
            this.faces = faces;
            chunkyTriMesh = new RcChunkyTriMesh(vertices, faces, faces.Length / 3, 32);
        }

        public int[] GetTris()
        {
            return faces;
        }

        public float[] GetVerts()
        {
            return vertices;
        }

        /// <summary>
        /// 根据给定的矩形边界，查询与矩形相交的分块三角形网格中的所有分块。
        /// 这个方法通过调用chunkyTriMesh实例的GetChunksOverlappingRect方法实现。
        /// </summary>
        /// <param name="bmin"></param>
        /// <param name="bmax"></param>
        /// <returns></returns>
        public List<RcChunkyTriMeshNode> GetChunksOverlappingRect(float[] bmin, float[] bmax)
        {
            return chunkyTriMesh.GetChunksOverlappingRect(bmin, bmax);
        }
    }
}