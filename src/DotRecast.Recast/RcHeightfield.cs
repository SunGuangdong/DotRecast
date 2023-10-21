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

namespace DotRecast.Recast
{
    /** Represents a heightfield layer within a layer set. */
    // 表示一个高度场层。一个高度场是一个二维网格，用于在导航网格构建过程中存储地形高度信息。类中包含以下属性：
    // 这个类通常用于在构建导航网格时存储地形高度信息。通过使用高度场，可以将地形划分为多个层，从而更容易地进行导航和碰撞检测计算。
    public class RcHeightfield
    {
        /** The width of the heightfield. (Along the x-axis in cell units.) */
        // 高度场的宽度（沿x轴的单元格单位）
        public readonly int width;

        /** The height of the heightfield. (Along the z-axis in cell units.) */
        // 高度场的高度（沿z轴的单元格单位）。
        public readonly int height;

        /** The minimum bounds in world space. [(x, y, z)] */
        // 世界空间中的最小边界坐标（一个RcVec3f对象，表示(x, y, z)）。
        public readonly RcVec3f bmin;

        /** The maximum bounds in world space. [(x, y, z)] */
        // 世界空间中的最大边界坐标（一个RcVec3f对象，表示(x, y, z)）。
        public RcVec3f bmax;

        /** The size of each cell. (On the xz-plane.) */
        // 每个单元格在xz平面上的大小。
        public readonly float cs;

        /** The height of each cell. (The minimum increment along the y-axis.) */
        // 每个单元格在y轴上的高度（最小增量）。
        public readonly float ch;

        /** Heightfield of spans (width*height). */
        // 跨度高度场（宽度*高度）。
        public readonly RcSpan[] spans;

        /** Border size in cell units */
        // 单元格单位的边界大小。
        public readonly int borderSize;

        /*
         * width：高度场的宽度。
            height：高度场的高度。
            bmin：世界空间中的最小边界坐标。
            bmax：世界空间中的最大边界坐标。
            cs：每个单元格在xz平面上的大小。
            ch：每个单元格在y轴上的高度。
            borderSize：单元格单位的边界大小。
         */
        public RcHeightfield(int width, int height, RcVec3f bmin, RcVec3f bmax, float cs, float ch, int borderSize)
        {
            this.width = width;
            this.height = height;
            this.bmin = bmin;
            this.bmax = bmax;
            this.cs = cs;
            this.ch = ch;
            this.borderSize = borderSize;
            // 根据给定的宽度和高度初始化spans数组。
            spans = new RcSpan[width * height];
        }
    }
}