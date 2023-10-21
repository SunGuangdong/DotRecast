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
    // 是一个表示凸体空间的类。凸体空间在导航网格生成过程中用于定义特殊区域，例如禁止通行区域或者具有特殊属性的区域。
    // 类主要用于在导航网格生成过程中定义特殊区域，以便在寻路时考虑这些区域的属性。
    public class RcConvexVolume
    {
        // 表示凸体空间顶点的数组。顶点坐标按x, y, z顺序存储，每3个元素表示一个顶点。
        public float[] verts;
        // 表示凸体空间在y轴方向上的最小高度
        public float hmin;
        // 表示凸体空间在y轴方向上的最大高度
        public float hmax;
        // 表示凸体空间的区域修改类型。这个类型用于指定凸体空间对应的导航网格区域的属性，例如禁止通行、行走缓慢等
        public RcAreaModification areaMod;
    }
}