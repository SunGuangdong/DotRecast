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
    /** Represents a span in a heightfield. */
    // 表示高度场中的一个跨度
    // 通常用于表示在高度场中的一个跨度，包含有关跨度的高度范围、区域ID和列中下一个跨度的信息。
    // 这些信息在构建导航网格时有助于确定可行走区域和连接区域。
    public class RcSpan
    {
        /** The lower limit of the span. [Limit: &lt; smax] */
        // 表示跨度的下限。限制条件：必须小于smax
        public int smin;

        /** The upper limit of the span. [Limit: &lt;= SPAN_MAX_HEIGHT] */
        // 表示跨度的上限。限制条件：必须小于等于SPAN_MAX_HEIGHT
        public int smax;

        /** The area id assigned to the span. */
        // 表示分配给跨度的区域ID
        public int area;

        /** The next span higher up in column. */
        // RcSpan对象，表示列中更高的下一个跨度
        public RcSpan next;
    }
}