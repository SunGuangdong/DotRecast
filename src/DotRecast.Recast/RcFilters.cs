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
using DotRecast.Core;

namespace DotRecast.Recast
{
    using static RcConstants;
    using static RcCommons;

    public static class RcFilters
    {
        /// @par
        ///用于过滤低悬挂的可行走障碍物。
        /// 这个方法可以用于在构建导航网格时过滤低悬挂的可行走障碍物，例如路缘和矮墙。通过允许代理在低矮物体上形成可行走区域，可以生成更符合实际需求的导航网格。
        /// 
        /// Allows the formation of walkable regions that will flow over low lying objects such as curbs, and up structures such as stairways.
        ///允许形成可步行区域，这些区域将流过路缘等低洼物体和楼梯等向上结构。
        /// Two neighboring spans are walkable if: <tt>RcAbs(currentSpan.smax - neighborSpan.smax) < walkableClimb</tt>
        ///两个相邻的跨度是可步行的，如果： <tt>RcAbs(currentSpan.smax - neighborSpan.smax) < walkableClimb</tt>
        /// @warning Will override the effect of #rcFilterLedgeSpans. So if both filters are used, call #rcFilterLedgeSpans after calling this filter.
        ///@warning 将覆盖 #rcFilterLedgeSpans 的效果。 因此，如果使用两个过滤器，请在调用此过滤器后调用#rcFilterLedgeSpans。
        /// @see rcHeightfield, rcConfig
        /*
         * ctx：一个RcTelemetry对象，用于测量过滤器的执行时间。
            walkableClimb：一个整数，表示代理能够攀爬的最大高度。
            solid：一个RcHeightfield对象，表示输入的高度场。
         */
        public static void FilterLowHangingWalkableObstacles(RcTelemetry ctx, int walkableClimb, RcHeightfield solid)
        {
            // 方法首先使用ctx.ScopedTimer测量过滤器的执行时间。
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_FILTER_LOW_OBSTACLES);

            int w = solid.width;
            int h = solid.height;

            // 然后，遍历高度场的所有跨度。对于每个跨度，检查其是否可行走（即区域不等于RC_NULL_AREA）。
            // 如果当前跨度不可行走，但其下方有一个可行走的跨度，并且两者之间的高度差小于等于walkableClimb，则将其上方的跨度也标记为可行走。
            // 为了防止可行走标志在多个不可行走对象之间传播，需要在每次迭代中复制可行走标志和区域。
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcSpan ps = null;
                    bool previousWalkable = false;
                    int previousArea = RC_NULL_AREA;

                    for (RcSpan s = solid.spans[x + y * w]; s != null; ps = s, s = s.next)
                    {
                        bool walkable = s.area != RC_NULL_AREA;
                        // If current span is not walkable, but there is walkable span just below it, mark the span above it walkable too.
                        // 如果当前跨度不可步行，但其下方有可步行跨度，则将其上方的跨度也标记为可步行。
                        if (!walkable && previousWalkable)
                        {
                            if (Math.Abs(s.smax - ps.smax) <= walkableClimb)
                                s.area = previousArea;
                        }

                        // Copy walkable flag so that it cannot propagate past multiple non-walkable objects.
                        // 复制可步行标志，使其无法传播经过多个不可步行对象。
                        previousWalkable = walkable;
                        previousArea = s.area;
                    }
                }
            }
        }

        /// @par
        ///用于过滤悬崖跨度。这个方法可以用于在构建导航网格时过滤悬崖跨度，防止生成的网格在悬崖上悬空。通过过滤悬崖跨度，可以生成更符合实际需求的导航网格。
        /// A ledge is a span with one or more neighbors whose maximum is further away than @p walkableClimb from the current span's maximum.
        /// 壁架是一个具有一个或多个邻居的跨度，其最大值距离当前跨度的最大值比 @p walkableClimb 更远。
        /// This method removes the impact of the overestimation of conservative voxelization
        /// so the resulting mesh will not have regions hanging in the air over ledges.
        ///此方法消除了保守体素化高估的影响，因此生成的网格不会有悬挂在壁架上方空气中的区域。
        /// A span is a ledge if: <tt>RcAbs(currentSpan.smax - neighborSpan.smax) > walkableClimb</tt>
        ///跨度是壁架 if: <tt>RcAbs(currentSpan.smax - neighborSpan.smax) > walkableClimb</tt>
        /// @see rcHeightfield, rcConfig
        /*
         * ctx：一个RcTelemetry对象，用于测量过滤器的执行时间。
            walkableHeight：一个整数，表示代理能够穿越的最大高度差。
            walkableClimb：一个整数，表示代理能够攀爬的最大高度。
            solid：一个RcHeightfield对象，表示输入的高度场。
         */
        public static void FilterLedgeSpans(RcTelemetry ctx, int walkableHeight, int walkableClimb, RcHeightfield solid)
        {
            // 方法首先使用ctx.ScopedTimer测量过滤器的执行时间。
            // 然后，遍历高度场的所有跨度。对于每个跨度，跳过不可行走的跨度。计算跨度的底部和顶部高度。然后，查找邻居的最小高度和可访问邻居的最小和最大高度。
            // 对于每个方向，检查邻居是否在边界内。如果在边界外，则更新minh。然后，检查跨度之间的间隙是否足够大。如果足够大，则更新minh。
            // 接下来，遍历邻居的所有跨度，检查跨度之间的间隙是否足够大，如果足够大，则更新minh。同时，查找最小/最大可访问邻居高度。
            // 如果任何相邻跨度的落差小于walkableClimb，则将当前跨度标记为RC_NULL_AREA。如果所有邻居之间的差异太大，则将跨度标记为RC_NULL_AREA。
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_FILTER_BORDER);

            int w = solid.width;
            int h = solid.height;

            // Mark border spans.  标记边界跨度。
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    for (RcSpan s = solid.spans[x + y * w]; s != null; s = s.next)
                    {
                        // Skip non walkable spans.  跳过不可步行的跨度。
                        if (s.area == RC_NULL_AREA)
                            continue;

                        int bot = (s.smax);
                        int top = s.next != null ? s.next.smin : SPAN_MAX_HEIGHT;

                        // Find neighbours minimum height.   找到邻居的最小高度。
                        int minh = SPAN_MAX_HEIGHT;

                        // Min and max height of accessible neighbours.  可到达的邻居的最小和最大高度。
                        int asmin = s.smax;
                        int asmax = s.smax;

                        for (int dir = 0; dir < 4; ++dir)
                        {
                            int dx = x + GetDirOffsetX(dir);
                            int dy = y + GetDirOffsetY(dir);
                            // Skip neighbours which are out of bounds.  跳过超出范围的邻居。
                            if (dx < 0 || dy < 0 || dx >= w || dy >= h)
                            {
                                minh = Math.Min(minh, -walkableClimb - bot);
                                continue;
                            }

                            // From minus infinity to the first span.   从负无穷大到第一个跨度。
                            RcSpan ns = solid.spans[dx + dy * w];
                            int nbot = -walkableClimb;
                            int ntop = ns != null ? ns.smin : SPAN_MAX_HEIGHT;
                            // Skip neightbour if the gap between the spans is too small. 如果跨度之间的间隙太小，则跳过邻居。
                            if (Math.Min(top, ntop) - Math.Max(bot, nbot) > walkableHeight)
                                minh = Math.Min(minh, nbot - bot);

                            // Rest of the spans.   其余的跨度。
                            for (ns = solid.spans[dx + dy * w]; ns != null; ns = ns.next)
                            {
                                nbot = ns.smax;
                                ntop = ns.next != null ? ns.next.smin : SPAN_MAX_HEIGHT;
                                // Skip neightbour if the gap between the spans is too small. 如果跨度之间的间隙太小，则跳过邻居。
                                if (Math.Min(top, ntop) - Math.Max(bot, nbot) > walkableHeight)
                                {
                                    minh = Math.Min(minh, nbot - bot);

                                    // Find min/max accessible neighbour height.  查找最小/最大可访问邻居高度。
                                    if (Math.Abs(nbot - bot) <= walkableClimb)
                                    {
                                        if (nbot < asmin)
                                            asmin = nbot;
                                        if (nbot > asmax)
                                            asmax = nbot;
                                    }
                                }
                            }
                        }

                        // 如果任何相邻跨度的落差小于 walkableClimb，则当前跨度接近壁架。
                        if (minh < -walkableClimb)
                            s.area = RC_NULL_AREA;

                        // 如果所有邻居之间的差异太大，则我们处于陡峭的斜坡上，将跨度标记为壁架。
                        if ((asmax - asmin) > walkableClimb)
                        {
                            s.area = RC_NULL_AREA;
                        }
                    }
                }
            }
        }

        /// @par
        ///用于过滤具有较低高度的可行走跨度。
        /// 这个方法可以用于在构建导航网格时过滤具有较低高度的可行走跨度，确保生成的网格仅包含足够空间供代理站立的区域。
        /// 通过过滤具有较低高度的可行走跨度，可以生成更符合实际需求的导航网格。
        /// For this filter, the clearance above the span is the distance from the span's
        /// maximum to the next higher span's minimum. (Same grid column.) 
        ///对于此过滤器，跨度上方的间隙是从跨度最大值到下一个更高跨度最小值的距离。 （相同的网格列。）
        /// @see rcHeightfield, rcConfig
        /*
         * ctx：一个RcTelemetry对象，用于测量过滤器的执行时间。
            walkableHeight：一个整数，表示代理能够穿越的最大高度差。
            solid：一个RcHeightfield对象，表示输入的高度场。
         */
        public static void FilterWalkableLowHeightSpans(RcTelemetry ctx, int walkableHeight, RcHeightfield solid)
        {
            //方法首先使用ctx.ScopedTimer测量过滤器的执行时间。
            //然后，遍历高度场的所有跨度。
            //对于每个跨度，计算其底部和顶部高度。
            //如果顶部和底部之间的高度差小于walkableHeight，则将跨度的区域设置为RC_NULL_AREA，表示它不可行走。
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_FILTER_WALKABLE);

            int w = solid.width;
            int h = solid.height;

            // Remove walkable flag from spans which do not have enough space above them for the agent to stand there.
            // 从跨度上移除可步行标志，因为跨度上方没有足够的空间供代理站在那里。
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    for (RcSpan s = solid.spans[x + y * w]; s != null; s = s.next)
                    {
                        int bot = (s.smax);
                        int top = s.next != null ? s.next.smin : SPAN_MAX_HEIGHT;
                        if ((top - bot) < walkableHeight)
                            s.area = RC_NULL_AREA;
                    }
                }
            }
        }
    }
}