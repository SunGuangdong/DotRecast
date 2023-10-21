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

namespace DotRecast.Recast
{
    using static RcConstants;
    using static RcCommons;

    // ，它包含一些用于处理高度场层的方法。这个类通常用于在构建导航网格时存储地形高度信息的子集。类中包含以下方法：
    
    // 这个类用于在导航网格构建过程中处理高度场层的数据。
    // BuildHeightfieldLayers方法是类中的主要方法，它从紧凑高度场构建高度场层集，这些层可以用于进一步构建导航网格。
    // RcLayers类可与RcHeightfieldLayer类和RcHeightfieldLayerSet类结合使用，以表示和操作高度场层的集合。
    public static class RcLayers
    {
        const int RC_MAX_LAYERS = RcConstants.RC_NOT_CONNECTED;
        const int RC_MAX_NEIS = 16;


        //向整数列表a中添加一个唯一的整数值v，如果v已经存在于列表中，则不添加。
        private static void AddUnique(List<int> a, int v)
        {
            if (!a.Contains(v))
            {
                a.Add(v);
            }
        }

        // 检查整数列表a是否包含整数值v，如果包含则返回true，否则返回false。
        private static bool Contains(List<int> a, int v)
        {
            return a.Contains(v);
        }

        // 检查两个范围是否重叠，如果重叠则返回true，否则返回false。
        private static bool OverlapRange(int amin, int amax, int bmin, int bmax)
        {
            return (amin > bmax || amax < bmin) ? false : true;
        }

        // 从RcCompactHeightfield对象构建RcHeightfieldLayerSet对象。该方法接受以下参数：
        /*
         * ctx：一个RcTelemetry对象，用于收集和报告构建过程中的性能数据。
            chf：一个RcCompactHeightfield对象，表示已经构建好的紧凑高度场。
            walkableHeight：一个整数，表示可行走区域的最小高度。
         */
        public static RcHeightfieldLayerSet BuildHeightfieldLayers(RcTelemetry ctx, RcCompactHeightfield chf, int walkableHeight)
        {
            // 该方法首先计算紧凑高度场中的区域，并将它们存储在RcLayerRegion数组中。然后，它通过合并相邻的高度范围相近的区域来创建2D层。
            // 最后，该方法为每个层创建一个RcHeightfieldLayer对象，并将它们存储在RcHeightfieldLayerSet对象中。
            // RcHeightfieldLayerSet对象可用于进一步构建导航网格，例如通过RcContourSet和RcPolyMesh对象。
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_BUILD_LAYERS);
            
            int w = chf.width;
            int h = chf.height;
            int borderSize = chf.borderSize;
            int[] srcReg = new int[chf.spanCount];
            Array.Fill(srcReg, 0xFF);
            int nsweeps = chf.width; // Math.Max(chf.width, chf.height);
            RcSweepSpan[] sweeps = new RcSweepSpan[nsweeps];
            for (int i = 0; i < sweeps.Length; i++)
            {
                sweeps[i] = new RcSweepSpan();
            }

            // Partition walkable area into monotone regions.  将步行区域划分为单调区域。
            int[] prevCount = new int[256];
            int regId = 0;
            // Sweep one line at a time.  一次扫一行。
            for (int y = borderSize; y < h - borderSize; ++y)
            {
                // Collect spans from this row.  收集该行的跨度。
                Array.Fill(prevCount, 0, 0, (regId) - (0));
                int sweepId = 0;

                for (int x = borderSize; x < w - borderSize; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];

                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];
                        if (chf.areas[i] == RC_NULL_AREA)
                            continue;
                        int sid = 0xFF;
                        // -x

                        if (GetCon(s, 0) != RC_NOT_CONNECTED)
                        {
                            int ax = x + GetDirOffsetX(0);
                            int ay = y + GetDirOffsetY(0);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 0);
                            if (chf.areas[ai] != RC_NULL_AREA && srcReg[ai] != 0xff)
                                sid = srcReg[ai];
                        }

                        if (sid == 0xff)
                        {
                            sid = sweepId++;
                            sweeps[sid].nei = 0xff;
                            sweeps[sid].ns = 0;
                        }

                        // -y
                        if (GetCon(s, 3) != RC_NOT_CONNECTED)
                        {
                            int ax = x + GetDirOffsetX(3);
                            int ay = y + GetDirOffsetY(3);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 3);
                            int nr = srcReg[ai];
                            if (nr != 0xff)
                            {
                                // Set neighbour when first valid neighbour is encoutered.  当遇到第一个有效邻居时设置邻居。
                                if (sweeps[sid].ns == 0)
                                    sweeps[sid].nei = nr;

                                if (sweeps[sid].nei == nr)
                                {
                                    // Update existing neighbour  更新现有邻居
                                    sweeps[sid].ns++;
                                    prevCount[nr]++;
                                }
                                else
                                {
                                    // This is hit if there is nore than one neighbour.
                                    // Invalidate the neighbour.
                                    // 如果有多个邻居，则会发生此情况。
                                    // // 使邻居无效。
                                    sweeps[sid].nei = 0xff;
                                }
                            }
                        }

                        srcReg[i] = sid;
                    }
                }

                // Create unique ID.  创建唯一 ID。
                for (int i = 0; i < sweepId; ++i)
                {
                    // If the neighbour is set and there is only one continuous connection to it,
                    // the sweep will be merged with the previous one, else new region is created.
                    // 如果邻居已设置并且只有一个连续连接到它，
                    // 该扫描将与前一个扫描合并，否则创建新区域。
                    if (sweeps[i].nei != 0xff && prevCount[sweeps[i].nei] == sweeps[i].ns)
                    {
                        sweeps[i].id = sweeps[i].nei;
                    }
                    else
                    {
                        if (regId == 255)
                        {
                            throw new Exception("rcBuildHeightfieldLayers: Region ID overflow.");
                        }

                        sweeps[i].id = regId++;
                    }
                }

                // Remap local sweep ids to region ids.  将本地扫描 ID 重新映射到区域 ID。
                for (int x = borderSize; x < w - borderSize; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        if (srcReg[i] != 0xff)
                            srcReg[i] = sweeps[srcReg[i]].id;
                    }
                }
            }

            int nregs = regId;
            RcLayerRegion[] regs = new RcLayerRegion[nregs];

            // Construct regions  构建区域
            for (int i = 0; i < nregs; ++i)
            {
                regs[i] = new RcLayerRegion(i);
            }

            // Find region neighbours and overlapping regions.  查找区域邻居和重叠区域。
            List<int> lregs = new List<int>();
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];

                    lregs.Clear();

                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];
                        int ri = srcReg[i];
                        if (ri == 0xff)
                            continue;

                        regs[ri].ymin = Math.Min(regs[ri].ymin, s.y);
                        regs[ri].ymax = Math.Max(regs[ri].ymax, s.y);

                        // Collect all region layers.   收集所有区域图层。
                        lregs.Add(ri);

                        // Update neighbours   Update neighbours
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RC_NOT_CONNECTED)
                            {
                                int ax = x + GetDirOffsetX(dir);
                                int ay = y + GetDirOffsetY(dir);
                                int ai = chf.cells[ax + ay * w].index + GetCon(s, dir);
                                int rai = srcReg[ai];
                                if (rai != 0xff && rai != ri)
                                    AddUnique(regs[ri].neis, rai);
                            }
                        }
                    }

                    // Update overlapping regions.    更新重叠区域。
                    for (int i = 0; i < lregs.Count - 1; ++i)
                    {
                        for (int j = i + 1; j < lregs.Count; ++j)
                        {
                            if (lregs[i] != lregs[j])
                            {
                                RcLayerRegion ri = regs[lregs[i]];
                                RcLayerRegion rj = regs[lregs[j]];
                                AddUnique(ri.layers, lregs[j]);
                                AddUnique(rj.layers, lregs[i]);
                            }
                        }
                    }
                }
            }

            // Create 2D layers from regions.    从区域创建 2D 图层。
            int layerId = 0;

            List<int> stack = new List<int>();

            for (int i = 0; i < nregs; ++i)
            {
                RcLayerRegion root = regs[i];
                // Skip already visited.   跳过已经访问过的。
                if (root.layerId != 0xff)
                    continue;

                // Start search.   开始寻找。
                root.layerId = layerId;
                root.@base = true;

                stack.Add(i);

                while (stack.Count != 0)
                {
                    // Pop front     
                    int pop = stack[0]; // TODO : 여기에 stack 처럼 작동하게 했는데, 스택인지는 모르겠음  我在这里让它像堆栈一样工作，但我不知道它是否是堆栈。
                    stack.RemoveAt(0);
                    RcLayerRegion reg = regs[pop];

                    foreach (int nei in reg.neis)
                    {
                        RcLayerRegion regn = regs[nei];
                        // Skip already visited.  跳过已经访问过的。
                        if (regn.layerId != 0xff)
                            continue;
                        // Skip if the neighbour is overlapping root region.  如果邻居与根区域重叠则跳过。
                        if (Contains(root.layers, nei))
                            continue;
                        // Skip if the height range would become too large.    如果高度范围变得太大，请跳过。
                        int ymin = Math.Min(root.ymin, regn.ymin);
                        int ymax = Math.Max(root.ymax, regn.ymax);
                        if ((ymax - ymin) >= 255)
                            continue;

                        // Deepen
                        stack.Add(nei);

                        // Mark layer id     标记图层id
                        regn.layerId = layerId;
                        // Merge current layers to root.   将当前图层合并到根。
                        foreach (int layer in regn.layers)
                            AddUnique(root.layers, layer);
                        root.ymin = Math.Min(root.ymin, regn.ymin);
                        root.ymax = Math.Max(root.ymax, regn.ymax);
                    }
                }

                layerId++;
            }

            // Merge non-overlapping regions that are close in height.    合并高度接近的非重叠区域。
            int mergeHeight = walkableHeight * 4;

            for (int i = 0; i < nregs; ++i)
            {
                RcLayerRegion ri = regs[i];
                if (!ri.@base)
                    continue;

                int newId = ri.layerId;

                for (;;)
                {
                    int oldId = 0xff;

                    for (int j = 0; j < nregs; ++j)
                    {
                        if (i == j)
                            continue;
                        RcLayerRegion rj = regs[j];
                        if (!rj.@base)
                            continue;

                        // Skip if the regions are not close to each other.  如果区域彼此不靠近，则跳过。
                        if (!OverlapRange(ri.ymin, ri.ymax + mergeHeight, rj.ymin, rj.ymax + mergeHeight))
                            continue;
                        // Skip if the height range would become too large.  如果高度范围变得太大，请跳过。
                        int ymin = Math.Min(ri.ymin, rj.ymin);
                        int ymax = Math.Max(ri.ymax, rj.ymax);
                        if ((ymax - ymin) >= 255)
                            continue;

                        // Make sure that there is no overlap when merging 'ri' and 'rj'.
                        // 确保合并 'ri' 和 'rj' 时没有重叠。
                        bool overlap = false;
                        // Iterate over all regions which have the same layerId as 'rj'
                        // 迭代与'rj'具有相同layerId的所有区域
                        for (int k = 0; k < nregs; ++k)
                        {
                            if (regs[k].layerId != rj.layerId)
                                continue;
                            // Check if region 'k' is overlapping region 'ri' Index to 'regs' is the same as region id.
                            // 检查区域 'k' 是否与区域 'ri' 重叠 'regs' 的索引与区域 id 相同。
                            if (Contains(ri.layers, k))
                            {
                                overlap = true;
                                break;
                            }
                        }

                        // Cannot merge of regions overlap.   无法合并区域重叠。
                        if (overlap)
                            continue;

                        // Can merge i and j.    可以合并 i 和 j。
                        oldId = rj.layerId;
                        break;
                    }

                    // Could not find anything to merge with, stop.    找不到任何可以合并的东西，停止。
                    if (oldId == 0xff)
                        break;

                    // Merge
                    for (int j = 0; j < nregs; ++j)
                    {
                        RcLayerRegion rj = regs[j];
                        if (rj.layerId == oldId)
                        {
                            rj.@base = false;
                            // Remap layerIds.     重新映射layerId。
                            rj.layerId = newId;
                            // Add overlaid layers from 'rj' to 'ri'.     添加从 'rj' 到 'ri' 的覆盖层。
                            foreach (int layer in rj.layers)
                                AddUnique(ri.layers, layer);
                            // Update height bounds.        更新高度界限。
                            ri.ymin = Math.Min(ri.ymin, rj.ymin);
                            ri.ymax = Math.Max(ri.ymax, rj.ymax);
                        }
                    }
                }
            }

            // Compact layerIds       紧凑的layerId
            int[] remap = new int[256];

            // Find number of unique layers.        找到独特层的数量。
            layerId = 0;
            for (int i = 0; i < nregs; ++i)
                remap[regs[i].layerId] = 1;
            for (int i = 0; i < 256; ++i)
            {
                if (remap[i] != 0)
                    remap[i] = layerId++;
                else
                    remap[i] = 0xff;
            }

            // Remap ids.        重新映射 id。
            for (int i = 0; i < nregs; ++i)
                regs[i].layerId = remap[regs[i].layerId];

            // No layers, return empty.       没有层，返回空。
            if (layerId == 0)
            {
                // ctx.Stop(RC_TIMER_BUILD_LAYERS);
                return null;
            }

            // Create layers.          创建图层。
            // RcAssert(lset.layers == 0);

            int lw = w - borderSize * 2;
            int lh = h - borderSize * 2;

            // Build contracted bbox for layers.     为图层构建收缩的 bbox。
            RcVec3f bmin = chf.bmin;
            RcVec3f bmax = chf.bmax;
            bmin.x += borderSize * chf.cs;
            bmin.z += borderSize * chf.cs;
            bmax.x -= borderSize * chf.cs;
            bmax.z -= borderSize * chf.cs;

            RcHeightfieldLayerSet lset = new RcHeightfieldLayerSet();
            lset.layers = new RcHeightfieldLayer[layerId];
            for (int i = 0; i < lset.layers.Length; i++)
            {
                lset.layers[i] = new RcHeightfieldLayer();
            }

            // Store layers.       存储图层。
            for (int i = 0; i < lset.layers.Length; ++i)
            {
                int curId = i;

                RcHeightfieldLayer layer = lset.layers[i];

                int gridSize = lw * lh;

                layer.heights = new int[gridSize];
                Array.Fill(layer.heights, 0xFF);
                layer.areas = new int[gridSize];
                layer.cons = new int[gridSize];

                // Find layer height bounds.       找到层高界限。
                int hmin = 0, hmax = 0;
                for (int j = 0; j < nregs; ++j)
                {
                    if (regs[j].@base && regs[j].layerId == curId)
                    {
                        hmin = regs[j].ymin;
                        hmax = regs[j].ymax;
                    }
                }

                layer.width = lw;
                layer.height = lh;
                layer.cs = chf.cs;
                layer.ch = chf.ch;

                // Adjust the bbox to fit the heightfield.  Adjust the bbox to fit the heightfield.
                layer.bmin = bmin;
                layer.bmax = bmax;
                layer.bmin.y = bmin.y + hmin * chf.ch;
                layer.bmax.y = bmin.y + hmax * chf.ch;
                layer.hmin = hmin;
                layer.hmax = hmax;

                // Update usable data region.         更新可用数据区域。
                layer.minx = layer.width;
                layer.maxx = 0;
                layer.miny = layer.height;
                layer.maxy = 0;

                // Copy height and area from compact heightfield.      从紧凑高度字段复制高度和面积。
                for (int y = 0; y < lh; ++y)
                {
                    for (int x = 0; x < lw; ++x)
                    {
                        int cx = borderSize + x;
                        int cy = borderSize + y;
                        RcCompactCell c = chf.cells[cx + cy * w];
                        for (int j = c.index, nj = c.index + c.count; j < nj; ++j)
                        {
                            RcCompactSpan s = chf.spans[j];
                            // Skip unassigned regions.         跳过未分配的区域。
                            if (srcReg[j] == 0xff)
                                continue;
                            // Skip of does nto belong to current layer.     跳过不属于当前层的内容。
                            int lid = regs[srcReg[j]].layerId;
                            if (lid != curId)
                                continue;

                            // Update data bounds.    更新数据范围。
                            layer.minx = Math.Min(layer.minx, x);
                            layer.maxx = Math.Max(layer.maxx, x);
                            layer.miny = Math.Min(layer.miny, y);
                            layer.maxy = Math.Max(layer.maxy, y);

                            // Store height and area type.        存储高度和区域类型。
                            int idx = x + y * lw;
                            layer.heights[idx] = (char)(s.y - hmin);
                            layer.areas[idx] = chf.areas[j];

                            // Check connection. 检查连接。
                            char portal = (char)0;
                            char con = (char)0;
                            for (int dir = 0; dir < 4; ++dir)
                            {
                                if (GetCon(s, dir) != RC_NOT_CONNECTED)
                                {
                                    int ax = cx + GetDirOffsetX(dir);
                                    int ay = cy + GetDirOffsetY(dir);
                                    int ai = chf.cells[ax + ay * w].index + GetCon(s, dir);
                                    int alid = srcReg[ai] != 0xff ? regs[srcReg[ai]].layerId : 0xff;
                                    // Portal mask      门户掩码
                                    if (chf.areas[ai] != RC_NULL_AREA && lid != alid)
                                    {
                                        portal |= (char)(1 << dir);
                                        // Update height so that it matches on both sides of the portal.
                                        // 更新高度，使其与门户两侧相匹配。
                                        RcCompactSpan @as = chf.spans[ai];
                                        if (@as.y > hmin)
                                            layer.heights[idx] = Math.Max(layer.heights[idx], (char)(@as.y - hmin));
                                    }

                                    // Valid connection mask            有效的连接掩码
                                    if (chf.areas[ai] != RC_NULL_AREA && lid == alid)
                                    {
                                        int nx = ax - borderSize;
                                        int ny = ay - borderSize;
                                        if (nx >= 0 && ny >= 0 && nx < lw && ny < lh)
                                            con |= (char)(1 << dir);
                                    }
                                }
                            }

                            layer.cons[idx] = (portal << 4) | con;
                        }
                    }
                }

                if (layer.minx > layer.maxx)
                    layer.minx = layer.maxx = 0;
                if (layer.miny > layer.maxy)
                    layer.miny = layer.maxy = 0;
            }

            // ctx->StopTimer(RC_TIMER_BUILD_LAYERS);
            return lset;
        }
    }
}