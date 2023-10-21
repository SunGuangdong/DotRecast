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

using System.Collections.Generic;
using DotRecast.Core;

namespace DotRecast.Recast
{
    /** Represents a group of related contours. */
    // 表示一组相关轮廓的类
    // 类主要用于存储导航网格生成过程中的轮廓数据，包括简化后的轮廓以及与轮廓相关的边界和尺寸信息。这些数据将用于后续的三角化和寻路操作。
    public class RcContourSet
    {
        /** A list of the contours in the set. */
        // 表示轮廓集合中的轮廓列表
        public List<RcContour> conts = new List<RcContour>();

        /** The minimum bounds in world space. [(x, y, z)] */
        // 表示轮廓集合在世界空间中的最小边界（x, y, z）
        public RcVec3f bmin = new RcVec3f();

        /** The maximum bounds in world space. [(x, y, z)] */
        // 表示轮廓集合在世界空间中的最大边界（x, y, z）
        public RcVec3f bmax = new RcVec3f();

        /** The size of each cell. (On the xz-plane.) */
        // 表示每个单元格在xz平面上的尺寸
        public float cs;

        /** The height of each cell. (The minimum increment along the y-axis.) */
        // 表示每个单元格在y轴方向上的高度（最小增量）
        public float ch;

        /** The width of the set. (Along the x-axis in cell units.) */
        // 表示轮廓集合在x轴方向上的宽度（以单元格单位计）
        public int width;

        /** The height of the set. (Along the z-axis in cell units.) */
        // 表示轮廓集合在z轴方向上的高度（以单元格单位计）
        public int height;

        /** The AABB border size used to generate the source data from which the contours were derived. */
        // 表示用于生成轮廓源数据的AABB边界大小
        public int borderSize;

        /** The max edge error that this contour set was simplified with. */
        // 表示轮廓集合在简化过程中允许的最大边缘误差
        public float maxError;
    }
}