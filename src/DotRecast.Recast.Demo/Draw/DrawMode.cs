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

namespace DotRecast.Recast.Demo.Draw;

/// <summary>
/// 定义了18个静态只读DrawMode对象，分别表示不同的绘制模式，如DRAWMODE_MESH表示输入网格，DRAWMODE_NAVMESH表示导航网格等。
/// </summary>
public class DrawMode
{
    // 表示绘制输入的网格。
    public static readonly DrawMode DRAWMODE_MESH = new(0, "Input Mesh");
    // 表示绘制导航网格。            【Poly Mesh的， 不是Poly Mesh Detils】
    public static readonly DrawMode DRAWMODE_NAVMESH = new(1, "Navmesh");
    // 表示绘制不可见的导航网格。      表现上很像Input Mesh （就是只绘制原始Mesh）
    public static readonly DrawMode DRAWMODE_NAVMESH_INVIS = new(2, "Navmesh Invis");
    // 表示绘制透明的导航网格。        （不会绘制原始Mesh，只有导航）
    public static readonly DrawMode DRAWMODE_NAVMESH_TRANS = new(3, "Navmesh Trans");
    // 表示绘制导航网格的BV树（包围体层次树）。
    public static readonly DrawMode DRAWMODE_NAVMESH_BVTREE = new(4, "Navmesh BVTree");
    // 表示绘制导航网格的节点。              【没看出差别】
    public static readonly DrawMode DRAWMODE_NAVMESH_NODES = new(5, "Navmesh Nodes");
    // 表示绘制导航网格的门户（连接不同区域的通道）。 【没看出差别】
    public static readonly DrawMode DRAWMODE_NAVMESH_PORTALS = new(6, "Navmesh Portals");
    // 表示绘制体素。      【很卡】
    public static readonly DrawMode DRAWMODE_VOXELS = new(7, "Voxels");
    // 表示绘制可行走的体素。   【在体素基础上绘制上表面？ 没有剔除不可行走的斜率三角形，】
    public static readonly DrawMode DRAWMODE_VOXELS_WALKABLE = new(8, "Walkable Voxels");
    // 表示绘制压缩后的网格。      【绘制上有类似波纹】
    public static readonly DrawMode DRAWMODE_COMPACT = new(9, "Compact");
    // 表示绘制压缩后的网格距离。    【灰白距离场， 边缘处最黑。越空旷越白】
    public static readonly DrawMode DRAWMODE_COMPACT_DISTANCE = new(10, "Compact Distance");
    // 表示绘制压缩后的网格区域。       【不同区域不同颜色】
    public static readonly DrawMode DRAWMODE_COMPACT_REGIONS = new(11, "Compact Regions");
    // 表示绘制区域连接。           【在上一步基础上 添加临接区域的箭头】
    public static readonly DrawMode DRAWMODE_REGION_CONNECTIONS = new(12, "Region Connections");
    // 表示绘制原始轮廓线。         【区域的边际线】
    public static readonly DrawMode DRAWMODE_RAW_CONTOURS = new(13, "Raw Contours");
    // 表示绘制原始和简化后的轮廓线。   【区域的边际线  +  简化后的边际线。 还没有Mesh】
    public static readonly DrawMode DRAWMODE_BOTH_CONTOURS = new(14, "Both Contours");
    // 表示绘制简化后的轮廓线。        【 只有简化后的边际线】
    public static readonly DrawMode DRAWMODE_CONTOURS = new(15, "Contours");
    // 表示绘制多边形网格。             【也就是可以看到粗略三角面了，整体导航色】
    public static readonly DrawMode DRAWMODE_POLYMESH = new(16, "Poly Mesh");
    // 表示绘制多边形网格的详细信息。     【不同区域颜色 + 分割了更多Mesh】
    public static readonly DrawMode DRAWMODE_POLYMESH_DETAIL = new(17, "Poly Mesh Detils");

    // 一个RcImmutableArray数组，包含所有的DrawMode对象。
    public static readonly RcImmutableArray<DrawMode> Values = RcImmutableArray.Create(
        DRAWMODE_MESH,
        DRAWMODE_NAVMESH,
        DRAWMODE_NAVMESH_INVIS,
        DRAWMODE_NAVMESH_TRANS,
        DRAWMODE_NAVMESH_BVTREE,
        DRAWMODE_NAVMESH_NODES,
        DRAWMODE_NAVMESH_PORTALS,
        DRAWMODE_VOXELS,
        DRAWMODE_VOXELS_WALKABLE,
        DRAWMODE_COMPACT,
        DRAWMODE_COMPACT_DISTANCE,
        DRAWMODE_COMPACT_REGIONS,
        DRAWMODE_REGION_CONNECTIONS,
        DRAWMODE_RAW_CONTOURS,
        DRAWMODE_BOTH_CONTOURS,
        DRAWMODE_CONTOURS,
        DRAWMODE_POLYMESH,
        DRAWMODE_POLYMESH_DETAIL
    );

    public int Idx { get; }
    public string Text { get; }

    private DrawMode(int idx, string text)
    {
        Idx = idx;
        Text = text;
    }

    public static DrawMode OfIdx(int idx)
    {
        return Values[idx];
    }

    public override string ToString()
    {
        return Text;
    }
}