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
    /// 这个类表示一个简单的输入几何提供器（Simple Input Geometry Provider），它实现了IInputGeomProvider接口，用于从三角形网格数据中创建导航网格。
    /// </summary>
    public class SimpleInputGeomProvider : IInputGeomProvider
    {
        /// <summary>
        /// 表示三角形网格的顶点坐标，是一个浮点数数组。数组中每三个元素表示一个顶点的x、y、z坐标。
        /// </summary>
        public readonly float[] vertices;
        /// <summary>
        /// 表示三角形网格的面信息，是一个整数数组。数组中每三个元素表示一个三角形面的三个顶点索引。
        /// </summary>
        public readonly int[] faces;
        /// <summary>
        /// 表示三角形网格的法向量，是一个浮点数数组。数组中每三个元素表示一个三角形面的法向量。
        /// </summary>
        public readonly float[] normals;
        /// <summary>
        /// 表示三角形网格的最小边界 
        /// </summary>
        private RcVec3f bmin;
        /// <summary>
        /// 表示三角形网格的最大边界 
        /// </summary>
        private RcVec3f bmax;

        /// <summary>
        /// 表示一个RcConvexVolume类型的列表，用于存储凸体区域。
        /// </summary>
        private readonly List<RcConvexVolume> volumes = new List<RcConvexVolume>();
        /// <summary>
        /// 表示一个RcTriMesh类型的实例，用于存储三角形网格数据。
        /// </summary>
        private readonly RcTriMesh _mesh;

        /// <summary>
        /// 从给定的.obj文件路径中加载三角形网格数据，并返回一个SimpleInputGeomProvider实例。
        /// </summary>
        /// <param name="objFilePath"></param>
        /// <returns></returns>
        public static SimpleInputGeomProvider LoadFile(string objFilePath)
        {
            byte[] chunk = RcResources.Load(objFilePath);
            var context = RcObjImporter.LoadContext(chunk);
            return new SimpleInputGeomProvider(context.vertexPositions, context.meshFaces);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="vertexPositions"></param>
        /// <param name="meshFaces"></param>
        public SimpleInputGeomProvider(List<float> vertexPositions, List<int> meshFaces)
            : this(MapVertices(vertexPositions), MapFaces(meshFaces))
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="meshFaces"></param>
        /// <returns></returns>
        private static int[] MapFaces(List<int> meshFaces)
        {
            int[] faces = new int[meshFaces.Count];
            for (int i = 0; i < faces.Length; i++)
            {
                faces[i] = meshFaces[i];
            }

            return faces;
        }

        
        private static float[] MapVertices(List<float> vertexPositions)
        {
            float[] vertices = new float[vertexPositions.Count];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertexPositions[i];
            }

            return vertices;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="faces"></param>
        public SimpleInputGeomProvider(float[] vertices, int[] faces)
        {
            this.vertices = vertices;
            this.faces = faces;
            normals = new float[faces.Length];
            CalculateNormals();
            bmin = RcVec3f.Zero;
            bmax = RcVec3f.Zero;
            RcVec3f.Copy(ref bmin, vertices, 0);
            RcVec3f.Copy(ref bmax, vertices, 0);
            for (int i = 1; i < vertices.Length / 3; i++)
            {
                bmin.Min(vertices, i * 3);
                bmax.Max(vertices, i * 3);
            }

            _mesh = new RcTriMesh(vertices, faces);
        }

        public RcTriMesh GetMesh()
        {
            return _mesh;
        }

        public RcVec3f GetMeshBoundsMin()
        {
            return bmin;
        }

        public RcVec3f GetMeshBoundsMax()
        {
            return bmax;
        }

        public IList<RcConvexVolume> ConvexVolumes()
        {
            return volumes;
        }

        
        /// <summary>
        /// 根据给定的顶点、高度、区域等信息，向凸体区域列表中添加一个新的RcConvexVolume实例。
        /// </summary>
        /// <param name="verts"></param>
        /// <param name="minh"></param>
        /// <param name="maxh"></param>
        /// <param name="areaMod"></param>
        public void AddConvexVolume(float[] verts, float minh, float maxh, RcAreaModification areaMod)
        {
            RcConvexVolume vol = new RcConvexVolume();
            vol.hmin = minh;
            vol.hmax = maxh;
            vol.verts = verts;
            vol.areaMod = areaMod;
        }

        /// <summary>
        /// 向凸体区域列表中添加一个已经创建好的RcConvexVolume实例
        /// </summary>
        /// <param name="convexVolume"></param>
        public void AddConvexVolume(RcConvexVolume convexVolume)
        {
            volumes.Add(convexVolume);
        }

        public IEnumerable<RcTriMesh> Meshes()
        {
            return RcImmutableArray.Create(_mesh);
        }

        public List<RcOffMeshConnection> GetOffMeshConnections()
        {
            throw new NotImplementedException();
        }

        public void AddOffMeshConnection(RcVec3f start, RcVec3f end, float radius, bool bidir, int area, int flags)
        {
            throw new NotImplementedException();
        }

        public void RemoveOffMeshConnections(Predicate<RcOffMeshConnection> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 计算三角形网格的法向量。这个方法使用了叉积（cross product）计算法向量，并对法向量进行归一化处理。
        /// </summary>
        public void CalculateNormals()
        {
            for (int i = 0; i < faces.Length; i += 3)
            {
                int v0 = faces[i] * 3;
                int v1 = faces[i + 1] * 3;
                int v2 = faces[i + 2] * 3;

                var e0 = new RcVec3f();
                var e1 = new RcVec3f();
                e0.x = vertices[v1 + 0] - vertices[v0 + 0];
                e0.y = vertices[v1 + 1] - vertices[v0 + 1];
                e0.z = vertices[v1 + 2] - vertices[v0 + 2];

                e1.x = vertices[v2 + 0] - vertices[v0 + 0];
                e1.y = vertices[v2 + 1] - vertices[v0 + 1];
                e1.z = vertices[v2 + 2] - vertices[v0 + 2];

                normals[i] = e0.y * e1.z - e0.z * e1.y;
                normals[i + 1] = e0.z * e1.x - e0.x * e1.z;
                normals[i + 2] = e0.x * e1.y - e0.y * e1.x;
                float d = (float)Math.Sqrt(normals[i] * normals[i] + normals[i + 1] * normals[i + 1] + normals[i + 2] * normals[i + 2]);
                if (d > 0)
                {
                    d = 1.0f / d;
                    normals[i] *= d;
                    normals[i + 1] *= d;
                    normals[i + 2] *= d;
                }
            }
        }
    }
}