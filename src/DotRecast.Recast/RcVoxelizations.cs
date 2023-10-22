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
using DotRecast.Recast.Geom;

namespace DotRecast.Recast
{
    public static class RcVoxelizations
    {
        // 用于根据输入几何数据、构建配置和遥测信息构建一个固体高度场（RcHeightfield）对象。
        // RcHeightfield对象可用于进一步构建导航网格，例如通过RcCompactHeightfield和RcHeightfieldLayerSet对象。
        /*
         * geomProvider：一个IInputGeomProvider接口的实例，表示输入的几何数据。
            builderCfg：一个RcBuilderConfig对象，包含构建配置信息。
            ctx：一个RcTelemetry对象，用于收集和报告构建过程中的性能数据。
         */
        public static RcHeightfield BuildSolidHeightfield(IInputGeomProvider geomProvider, RcBuilderConfig builderCfg, RcTelemetry ctx)
        {
            // 方法首先创建一个RcHeightfield对象，用于存储栅格化后的输入数据。
            // 然后，它遍历输入几何数据中的所有三角形，根据它们的坡度找到可行走的三角形，并将它们栅格化到高度场中。
            // 如果输入数据是多个网格，可以在此方法中对它们进行变换、计算每个网格的区域类型并栅格化它们。
            RcConfig cfg = builderCfg.cfg;

            // Allocate voxel heightfield where we rasterize our input data to.
            // 在我们栅格化输入数据的位置分配体素高度场。
            RcHeightfield solid = new RcHeightfield(builderCfg.width, builderCfg.height, builderCfg.bmin, builderCfg.bmax, cfg.Cs, cfg.Ch, cfg.BorderSize);

            // Allocate array that can hold triangle area types.
            // If you have multiple meshes you need to process,
            // allocate and array which can hold the max number of triangles you need to process.
            // 分配可以保存三角形区域类型的数组。
            // 如果您需要处理多个网格，
            // 分配一个可以容纳需要处理的最大三角形数量的数组。

            // Find triangles which are walkable based on their slope and rasterize them.
            // If your input data is multiple meshes, you can transform them here,
            // calculate the are type for each of the meshes and rasterize them.
            // 根据坡度找到可行走的三角形并将其栅格化。
            // 如果您的输入数据是多个网格，您可以在这里对其进行转换，
            // 计算每个网格的面积类型并对它们进行栅格化。
            foreach (RcTriMesh geom in geomProvider.Meshes())
            {
                float[] verts = geom.GetVerts();
                if (cfg.UseTiles)
                {
                    float[] tbmin = new float[2];
                    float[] tbmax = new float[2];
                    tbmin[0] = builderCfg.bmin.x;
                    tbmin[1] = builderCfg.bmin.z;
                    tbmax[0] = builderCfg.bmax.x;
                    tbmax[1] = builderCfg.bmax.z;
                    List<RcChunkyTriMeshNode> nodes = geom.GetChunksOverlappingRect(tbmin, tbmax);
                    foreach (RcChunkyTriMeshNode node in nodes)
                    {
                        int[] tris = node.tris;
                        int ntris = tris.Length / 3;
                        int[] m_triareas = RcCommons.MarkWalkableTriangles(ctx, cfg.WalkableSlopeAngle, verts, tris, ntris, cfg.WalkableAreaMod);
                        RcRasterizations.RasterizeTriangles(solid, verts, tris, m_triareas, ntris, cfg.WalkableClimb, ctx);
                    }
                }
                else
                {
                    int[] tris = geom.GetTris();
                    int ntris = tris.Length / 3;
                    int[] m_triareas = RcCommons.MarkWalkableTriangles(ctx, cfg.WalkableSlopeAngle, verts, tris, ntris, cfg.WalkableAreaMod);
                    RcRasterizations.RasterizeTriangles(solid, verts, tris, m_triareas, ntris, cfg.WalkableClimb, ctx);
                }
            }

            return solid;
        }
    }
}
