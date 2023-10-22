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

namespace DotRecast.Recast.Demo.Draw;

/// <summary>
/// 用于表示调试绘制时使用的几何图元类型。这些图元类型通常用于可视化调试，例如在游戏或仿真中绘制物体的边界框、网格或其他辅助信息。
/// 在实际应用中，DebugDrawPrimitives枚举值可以与调试绘制函数一起使用，以便根据需要绘制不同类型的图元。
/// 例如，可以使用POINTS绘制粒子系统中的粒子，使用LINES绘制物体的边界框，使用TRIS绘制多边形网格，以及使用QUADS绘制屏幕上的二维图形。
/// </summary>
public enum DebugDrawPrimitives
{
    //表示点图元。点通常用于表示单个顶点或离散的位置，例如粒子系统中的粒子或三维空间中的控制点。
    POINTS,
    //表示线图元。线通常用于表示物体之间的连接或物体的轮廓，例如骨骼动画中的骨骼连接或物体的边界框。
    LINES,
    //表示三角形图元。三角形通常用于表示三维物体的表面，例如多边形网格或地形。
    TRIS,
    //表示四边形图元。四边形通常用于表示平面或矩形区域，例如纹理贴图或屏幕上的二维图形。
    QUADS
}