/*
recast4j copyright (c) 2021 Piotr Piastucki piotr@jtilia.org
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
using DotRecast.Core;

namespace DotRecast.Recast
{
    /// <summary>
    /// 提供了在多边形网格（poly mesh）和多边形网格细节（poly mesh detail）中进行射线投射的方法。
    /// 射线投射在导航网格中的应用包括检查两个点之间是否有遮挡物，以及寻找最近的可见点等。
    ///
    /// 这个类提供了在导航网格中进行射线投射的方法，可以用于检查两个点之间的可见性，以及寻找最近的可见点等。这些功能在寻路和避障等场景中非常有用。
    /// </summary>
    public static class RcPolyMeshRaycast
    {
        /// <summary>
        /// 这是一个公共静态方法，用于在一组构建结果（RcBuilderResult）中进行射线投射。
        /// 方法接受一个 RcBuilderResult 列表，一个源点 src 和一个目标点 dst。
        /// 方法返回一个布尔值，表示射线是否与网格相交；hitTime 输出参数表示相交点在射线上的时间参数。
        /// </summary>
        /// <param name="results"></param>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="hitTime"></param>
        /// <returns></returns>
        public static bool Raycast(IList<RcBuilderResult> results, RcVec3f src, RcVec3f dst, out float hitTime)
        {
            hitTime = 0.0f;
            //方法遍历所有构建结果，对于每一个包含多边形网格细节（RcPolyMeshDetail）的结果，调用 Raycast 私有方法进行射线投射。
            //如果射线与任何一个网格相交，方法返回 true，否则返回 false。
            foreach (RcBuilderResult result in results)
            {
                if (result.GetMeshDetail() != null)
                {
                    if (Raycast(result.GetMesh(), result.GetMeshDetail(), src, dst, out hitTime))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 用于在给定的多边形网格（RcPolyMesh）和多边形网格细节（RcPolyMeshDetail）中进行射线投射。
        /// 方法接受一个多边形网格 poly，一个多边形网格细节 meshDetail，一个源点 sp 和一个目标点 sq。
        /// 方法返回一个布尔值，表示射线是否与网格相交；hitTime 输出参数表示相交点在射线上的时间参数。
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="meshDetail"></param>
        /// <param name="sp"></param>
        /// <param name="sq"></param>
        /// <param name="hitTime"></param>
        /// <returns></returns>
        private static bool Raycast(RcPolyMesh poly, RcPolyMeshDetail meshDetail, RcVec3f sp, RcVec3f sq, out float hitTime)
        {
            hitTime = 0;
            if (meshDetail != null)
            {
//方法首先检查 meshDetail 是否为空。如果不为空，遍历所有的子网格，并对每一个子网格中的三角形进行射线-三角形相交测试。
//如果射线与任何一个三角形相交，方法返回 true，否则返回 false。
//如果 meshDetail 为空，方法暂时返回 false，表示未实现对多边形网格（RcPolyMesh）的射线投射。
                for (int i = 0; i < meshDetail.nmeshes; ++i)
                {
                    int m = i * 4;
                    int bverts = meshDetail.meshes[m];
                    int btris = meshDetail.meshes[m + 2];
                    int ntris = meshDetail.meshes[m + 3];
                    int verts = bverts * 3;
                    int tris = btris * 4;
                    for (int j = 0; j < ntris; ++j)
                    {
                        RcVec3f[] vs = new RcVec3f[3];
                        for (int k = 0; k < 3; ++k)
                        {
                            vs[k].x = meshDetail.verts[verts + meshDetail.tris[tris + j * 4 + k] * 3];
                            vs[k].y = meshDetail.verts[verts + meshDetail.tris[tris + j * 4 + k] * 3 + 1];
                            vs[k].z = meshDetail.verts[verts + meshDetail.tris[tris + j * 4 + k] * 3 + 2];
                        }

                        if (RcIntersections.IntersectSegmentTriangle(sp, sq, vs[0], vs[1], vs[2], out hitTime))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                // TODO: check PolyMesh instead
            }

            return false;
        }
    }
}