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
using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Geom;

namespace DotRecast.Recast.Demo
{
    /// <summary>
    /// 它用于在3D环境中处理导航网格相关的信息。
    /// </summary>
    public class DemoSample
    {
        //存储DemoInputGeomProvider对象，该对象提供了与导航网格相关的几何信息。
        private DemoInputGeomProvider _geom;
        // 存储DtNavMesh对象，表示导航网格。
        private DtNavMesh _navMesh;
        // 存储DtNavMeshQuery对象，用于查询导航网格。
        private DtNavMeshQuery _navMeshQuery;
        // 存储RcNavMeshBuildSettings对象，表示导航网格的构建设置。
        private readonly RcNavMeshBuildSettings _settings;
        // 存储一个RcBuilderResult对象列表，表示导航网格构建的结果。
        private IList<RcBuilderResult> _recastResults;
        // 表示导航网格是否已更改。
        private bool _changed;

        // 构造函数，用于初始化DemoSample对象。
        public DemoSample(DemoInputGeomProvider geom, IList<RcBuilderResult> recastResults, DtNavMesh navMesh)
        {
            _geom = geom;
            _recastResults = recastResults;
            _navMesh = navMesh;
            _settings = new RcNavMeshBuildSettings();

            SetQuery(navMesh);
            _changed = true;
        }

        // 此方法用于设置DtNavMeshQuery对象。
        private void SetQuery(DtNavMesh navMesh)
        {
            _navMeshQuery = navMesh != null ? new DtNavMeshQuery(navMesh) : null;
        }

        public DemoInputGeomProvider GetInputGeom()
        {
            return _geom;
        }

        public IList<RcBuilderResult> GetRecastResults()
        {
            return _recastResults;
        }

        public DtNavMesh GetNavMesh()
        {
            return _navMesh;
        }

        public RcNavMeshBuildSettings GetSettings()
        {
            return _settings;
        }

        public DtNavMeshQuery GetNavMeshQuery()
        {
            return _navMeshQuery;
        }

        public bool IsChanged()
        {
            return _changed;
        }

        public void SetChanged(bool changed)
        {
            _changed = changed;
        }

        /// <summary>
        /// 此方法用于更新导航网格相关的信息。它接受一个DemoInputGeomProvider对象、一个RcBuilderResult对象列表和一个DtNavMesh对象作为参数。
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="recastResults"></param>
        /// <param name="navMesh"></param>
        /// <returns></returns>
        public void Update(DemoInputGeomProvider geom, IList<RcBuilderResult> recastResults, DtNavMesh navMesh)
        {
            _geom = geom;
            _recastResults = recastResults;
            _navMesh = navMesh;
            SetQuery(navMesh);

            _changed = true;

            // // by update
            // _inputGeom.ClearConvexVolumes();
            // _inputGeom.RemoveOffMeshConnections(x => true);
            //
            // if (null != _navMesh && 0 < _navMesh.GetTileCount())
            // {
            //     for (int ti = 0; ti < _navMesh.GetTileCount(); ++ti)
            //     {
            //         var tile = _navMesh.GetTile(ti);
            //         for (int pi = 0; pi < tile.data.polys.Length; ++pi)
            //         {
            //             var polyType = tile.data.polys[pi].GetPolyType();
            //             var polyArea= tile.data.polys[pi].GetArea();
            //
            //             if (0 != polyType)
            //             {
            //                 int a = 3;
            //             }
            //
            //             if (0 != polyArea)
            //             {
            //                 int b = 3;
            //             }
            //             
            //             Logger.Error($"tileIdx({ti}) polyIdx({pi}) polyType({polyType} polyArea({polyArea})");
            //         }
            //         
            //     }
            // }
        }
    }
}