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

using DotRecast.Core;

namespace DotRecast.Recast.Geom
{
    /// <summary>
    /// 表示一个离网连接（Off-Mesh Connection），离网连接是指在导航网格中两个不相邻的区域之间的连接，例如跳跃、攀爬等。
    /// </summary>
    public class RcOffMeshConnection
    {
        /// <summary>
        /// 表示离网连接的起点和终点的坐标，是一个长度为6的浮点数数组。
        /// 数组前三个元素表示起点的x、y、z坐标，后三个元素表示终点的x、y、z坐标。
        /// </summary>
        public readonly float[] verts;
        /// <summary>
        /// 表示离网连接的有效半径，即代理（Agent）在距离连接起点和终点小于等于此半径时，可以使用这个离网连接。
        /// </summary>
        public readonly float radius;

        /// <summary>
        /// 表示离网连接是否是双向的。如果为true，则代理可以从起点到终点，也可以从终点到起点；如果为false，则代理只能从起点到终点。
        /// </summary>
        public readonly bool bidir;
        /// <summary>
        /// 表示离网连接所在的区域ID，用于区分不同的导航区域。  注意是枚举的id 
        /// </summary>
        public readonly int area;

        /// <summary>
        /// 表示离网连接的标志，用于存储一些额外的信息，例如连接的类型、难度等。
        /// </summary>
        public readonly int flags;
        // userId：表示离网连接的用户ID，通常用于在游戏或应用程序中唯一标识这个离网连接。
        public readonly int userId;

        public RcOffMeshConnection(RcVec3f start, RcVec3f end, float radius, bool bidir, int area, int flags)
        {
            verts = new float[6];
            verts[0] = start.x;
            verts[1] = start.y;
            verts[2] = start.z;
            verts[3] = end.x;
            verts[4] = end.y;
            verts[5] = end.z;
            this.radius = radius;
            this.bidir = bidir;
            this.area = area;
            this.flags = flags;
        }
    }
}