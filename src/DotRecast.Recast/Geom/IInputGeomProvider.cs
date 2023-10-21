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

namespace DotRecast.Recast.Geom
{
    /// <summary>
    /// 接口定义了一个输入几何提供器（Input Geometry Provider），它用于从三角形网格数据中创建导航网格。
    /// </summary>
    public interface IInputGeomProvider
    {
        /// <summary>
        /// 返回一个RcTriMesh实例，表示三角形网格数据。
        /// </summary>
        /// <returns></returns>
        RcTriMesh GetMesh();
        /// <summary>
        /// 返回三角形网格的最小边界
        /// </summary>
        /// <returns></returns>
        RcVec3f GetMeshBoundsMin();

        /// <summary>
        /// 返回三角形网格的最大边界
        /// </summary>
        /// <returns></returns>
        RcVec3f GetMeshBoundsMax();

        /// <summary>
        /// 返回一个包含RcTriMesh实例的可枚举集合
        /// </summary>
        /// <returns></returns>
        IEnumerable<RcTriMesh> Meshes();
        
        // convex volume
        /// <summary>
        /// 向凸体区域列表中添加一个已经创建好的RcConvexVolume实例
        /// </summary>
        /// <param name="convexVolume"></param>
        void AddConvexVolume(RcConvexVolume convexVolume);
        
        /// <summary>
        /// 返回一个包含RcConvexVolume实例的列表，表示凸体区域。
        /// </summary>
        /// <returns></returns>
        IList<RcConvexVolume> ConvexVolumes();

        // off mesh connections
        /// <summary>
        /// 返回一个包含RcOffMeshConnection实例的列表，表示离网连接。
        /// </summary>
        /// <returns></returns>
        public List<RcOffMeshConnection> GetOffMeshConnections();
        /// <summary>
        /// 根据给定的起点、终点、半径、双向标志、区域ID和标志，向离网连接列表中添加一个新的RcOffMeshConnection实例。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="radius"></param>
        /// <param name="bidir"></param>
        /// <param name="area"></param>
        /// <param name="flags"></param>
        public void AddOffMeshConnection(RcVec3f start, RcVec3f end, float radius, bool bidir, int area, int flags);
        /// <summary>
        /// 根据给定的条件过滤器（Predicate），从离网连接列表中移除满足条件的离网连接。
        /// </summary>
        /// <param name="filter"></param>
        public void RemoveOffMeshConnections(Predicate<RcOffMeshConnection> filter);

    }
}