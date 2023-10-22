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
using System.Linq;
using DotRecast.Core;

namespace DotRecast.Recast
{
    using static RcConstants;
    using static RcCommons;

    /// <summary>
    /// 静态类，包含一组用于处理导航网格中区域的方法。
    /// 这些方法可用于在构建导航网格时处理和操作紧凑高度场中的区域。这些操作有助于确定可行走区域、连接区域以及处理重叠和边界连接。
    /// 通过使用RcRegions类，可以更容易地处理导航网格中的不同区域，以便在寻路和导航过程中找到最佳路径。
    /// </summary>
    public static class RcRegions
    {
        const int RC_NULL_NEI = 0xffff;

        // 计算紧凑高度场（RcCompactHeightfield）中每个跨度（RcCompactSpan）到最近边界的距离。
        // 方法首先初始化距离数组，然后标记边界单元格。接下来，它进行两次遍历操作以计算每个跨度到边界的距离。最后，返回最大距离。
        public static int CalculateDistanceField(RcCompactHeightfield chf, int[] src)
        {
            int maxDist;
            int w = chf.width;
            int h = chf.height;

            // Init distance and points.
            // 初始距离和点。
            for (int i = 0; i < chf.spanCount; ++i)
            {
                src[i] = 0xffff;
            }

            // Mark boundary cells.   标记边界单元。
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];
                        int area = chf.areas[i];

                        int nc = 0;
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RC_NOT_CONNECTED)
                            {
                                int ax = x + GetDirOffsetX(dir);
                                int ay = y + GetDirOffsetY(dir);
                                int ai = chf.cells[ax + ay * w].index + GetCon(s, dir);
                                if (area == chf.areas[ai])
                                {
                                    nc++;
                                }
                            }
                        }

                        if (nc != 4)
                        {
                            src[i] = 0;
                        }
                    }
                }
            }

            // Pass 1
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];

                        if (GetCon(s, 0) != RC_NOT_CONNECTED)
                        {
                            // (-1,0)
                            int ax = x + GetDirOffsetX(0);
                            int ay = y + GetDirOffsetY(0);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 0);
                            RcCompactSpan @as = chf.spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = src[ai] + 2;
                            }

                            // (-1,-1)
                            if (GetCon(@as, 3) != RC_NOT_CONNECTED)
                            {
                                int aax = ax + GetDirOffsetX(3);
                                int aay = ay + GetDirOffsetY(3);
                                int aai = chf.cells[aax + aay * w].index + GetCon(@as, 3);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = src[aai] + 3;
                                }
                            }
                        }

                        if (GetCon(s, 3) != RC_NOT_CONNECTED)
                        {
                            // (0,-1)
                            int ax = x + GetDirOffsetX(3);
                            int ay = y + GetDirOffsetY(3);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 3);
                            RcCompactSpan @as = chf.spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = src[ai] + 2;
                            }

                            // (1,-1)
                            if (GetCon(@as, 2) != RC_NOT_CONNECTED)
                            {
                                int aax = ax + GetDirOffsetX(2);
                                int aay = ay + GetDirOffsetY(2);
                                int aai = chf.cells[aax + aay * w].index + GetCon(@as, 2);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = src[aai] + 3;
                                }
                            }
                        }
                    }
                }
            }

            // Pass 2
            for (int y = h - 1; y >= 0; --y)
            {
                for (int x = w - 1; x >= 0; --x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];

                        if (GetCon(s, 2) != RC_NOT_CONNECTED)
                        {
                            // (1,0)
                            int ax = x + GetDirOffsetX(2);
                            int ay = y + GetDirOffsetY(2);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 2);
                            RcCompactSpan @as = chf.spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = src[ai] + 2;
                            }

                            // (1,1)
                            if (GetCon(@as, 1) != RC_NOT_CONNECTED)
                            {
                                int aax = ax + GetDirOffsetX(1);
                                int aay = ay + GetDirOffsetY(1);
                                int aai = chf.cells[aax + aay * w].index + GetCon(@as, 1);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = src[aai] + 3;
                                }
                            }
                        }

                        if (GetCon(s, 1) != RC_NOT_CONNECTED)
                        {
                            // (0,1)
                            int ax = x + GetDirOffsetX(1);
                            int ay = y + GetDirOffsetY(1);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 1);
                            RcCompactSpan @as = chf.spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = src[ai] + 2;
                            }

                            // (-1,1)
                            if (GetCon(@as, 0) != RC_NOT_CONNECTED)
                            {
                                int aax = ax + GetDirOffsetX(0);
                                int aay = ay + GetDirOffsetY(0);
                                int aai = chf.cells[aax + aay * w].index + GetCon(@as, 0);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = src[aai] + 3;
                                }
                            }
                        }
                    }
                }
            }

            maxDist = 0;
            for (int i = 0; i < chf.spanCount; ++i)
            {
                maxDist = Math.Max(src[i], maxDist);
            }

            return maxDist;
        }

        // 对紧凑高度场执行盒模糊操作以平滑区域边界。
        // 方法首先创建一个目标数组，然后遍历高度场中的每个跨度，计算邻近跨度的加权平均距离，并将结果存储在目标数组中。最后返回平滑后的目标数组。
        private static int[] BoxBlur(RcCompactHeightfield chf, int thr, int[] src)
        {
            int w = chf.width;
            int h = chf.height;
            int[] dst = new int[chf.spanCount];

            thr *= 2;

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];
                        int cd = src[i];
                        if (cd <= thr)
                        {
                            dst[i] = cd;
                            continue;
                        }

                        int d = cd;
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RC_NOT_CONNECTED)
                            {
                                int ax = x + GetDirOffsetX(dir);
                                int ay = y + GetDirOffsetY(dir);
                                int ai = chf.cells[ax + ay * w].index + GetCon(s, dir);
                                d += src[ai];

                                RcCompactSpan @as = chf.spans[ai];
                                int dir2 = (dir + 1) & 0x3;
                                if (GetCon(@as, dir2) != RC_NOT_CONNECTED)
                                {
                                    int ax2 = ax + GetDirOffsetX(dir2);
                                    int ay2 = ay + GetDirOffsetY(dir2);
                                    int ai2 = chf.cells[ax2 + ay2 * w].index + GetCon(@as, dir2);
                                    d += src[ai2];
                                }
                                else
                                {
                                    d += cd;
                                }
                            }
                            else
                            {
                                d += cd * 2;
                            }
                        }

                        dst[i] = ((d + 5) / 9);
                    }
                }
            }

            return dst;
        }

        // 使用洪水填充算法为给定的紧凑高度场中的一个跨度分配一个新的区域ID。
        // 方法首先清空堆栈并将初始跨度添加到堆栈中。然后遍历堆栈中的跨度，检查它们的邻居是否已经有一个有效的区域ID。
        // 如果找到一个邻居具有不同的区域ID，则将当前跨度的区域ID设置为0。
        // 否则，将当前跨度的区域ID设置为给定的新区域ID，并将其邻居添加到堆栈中。最后返回是否成功分配了区域ID。
        private static bool FloodRegion(int x, int y, int i, int level, int r, RcCompactHeightfield chf, int[] srcReg,
            int[] srcDist, List<int> stack)
        {
            int w = chf.width;

            int area = chf.areas[i];

            // Flood fill mark region.          洪水填充标记区域。
            stack.Clear();
            stack.Add(x);
            stack.Add(y);
            stack.Add(i);
            srcReg[i] = r;
            srcDist[i] = 0;

            int lev = level >= 2 ? level - 2 : 0;
            int count = 0;

            while (stack.Count > 0)
            {
                int ci = stack[^1];
                stack.RemoveAt(stack.Count - 1);

                int cy = stack[^1];
                stack.RemoveAt(stack.Count - 1);

                int cx = stack[^1];
                stack.RemoveAt(stack.Count - 1);


                RcCompactSpan cs = chf.spans[ci];

                // Check if any of the neighbours already have a valid region set.
                //  检查是否有任何邻居已经设置了有效的区域。
                int ar = 0;
                for (int dir = 0; dir < 4; ++dir)
                {
                    // 8 connected     // 8 个已连接
                    if (GetCon(cs, dir) != RC_NOT_CONNECTED)
                    {
                        int ax = cx + GetDirOffsetX(dir);
                        int ay = cy + GetDirOffsetY(dir);
                        int ai = chf.cells[ax + ay * w].index + GetCon(cs, dir);
                        if (chf.areas[ai] != area)
                        {
                            continue;
                        }

                        int nr = srcReg[ai];
                        if ((nr & RC_BORDER_REG) != 0)
                        {
                            continue;
                        }

                        if (nr != 0 && nr != r)
                        {
                            ar = nr;
                            break;
                        }

                        RcCompactSpan @as = chf.spans[ai];

                        int dir2 = (dir + 1) & 0x3;
                        if (GetCon(@as, dir2) != RC_NOT_CONNECTED)
                        {
                            int ax2 = ax + GetDirOffsetX(dir2);
                            int ay2 = ay + GetDirOffsetY(dir2);
                            int ai2 = chf.cells[ax2 + ay2 * w].index + GetCon(@as, dir2);
                            if (chf.areas[ai2] != area)
                            {
                                continue;
                            }

                            int nr2 = srcReg[ai2];
                            if (nr2 != 0 && nr2 != r)
                            {
                                ar = nr2;
                                break;
                            }
                        }
                    }
                }

                if (ar != 0)
                {
                    srcReg[ci] = 0;
                    continue;
                }

                count++;

                // Expand neighbours.      // 扩展邻居。
                for (int dir = 0; dir < 4; ++dir)
                {
                    if (GetCon(cs, dir) != RC_NOT_CONNECTED)
                    {
                        int ax = cx + GetDirOffsetX(dir);
                        int ay = cy + GetDirOffsetY(dir);
                        int ai = chf.cells[ax + ay * w].index + GetCon(cs, dir);
                        if (chf.areas[ai] != area)
                        {
                            continue;
                        }

                        if (chf.dist[ai] >= lev && srcReg[ai] == 0)
                        {
                            srcReg[ai] = r;
                            srcDist[ai] = 0;
                            stack.Add(ax);
                            stack.Add(ay);
                            stack.Add(ai);
                        }
                    }
                }
            }

            return count > 0;
        }

        // 扩展紧凑高度场中的区域，以填充未分配区域ID的跨度。方法首先根据fillStack参数确定是否使用输入堆栈或创建一个新堆栈。
        // 然后遍历堆栈中的跨度，尝试为每个跨度分配一个邻近跨度的区域ID。
        // 如果在最大迭代次数内未能为所有跨度分配区域ID，则停止扩展。最后返回包含分配的区域ID的源数组。
        private static int[] ExpandRegions(int maxIter, int level, RcCompactHeightfield chf, int[] srcReg, int[] srcDist,
            List<int> stack, bool fillStack)
        {
            int w = chf.width;
            int h = chf.height;

            if (fillStack)
            {
                // Find cells revealed by the raised level.
                // 查找由升高的级别显示的单元格。
                stack.Clear();
                for (int y = 0; y < h; ++y)
                {
                    for (int x = 0; x < w; ++x)
                    {
                        RcCompactCell c = chf.cells[x + y * w];
                        for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                        {
                            if (chf.dist[i] >= level && srcReg[i] == 0 && chf.areas[i] != RC_NULL_AREA)
                            {
                                stack.Add(x);
                                stack.Add(y);
                                stack.Add(i);
                            }
                        }
                    }
                }
            }
            else // use cells in the input stack   // 使用输入堆栈中的单元
            {
                // mark all cells which already have a region     // 标记所有已经有区域的单元格
                for (int j = 0; j < stack.Count; j += 3)
                {
                    int i = stack[j + 2];
                    if (srcReg[i] != 0)
                    {
                        stack[j + 2] = -1;
                    }
                }
            }

            List<int> dirtyEntries = new List<int>();
            int iter = 0;
            while (stack.Count > 0)
            {
                int failed = 0;
                dirtyEntries.Clear();

                for (int j = 0; j < stack.Count; j += 3)
                {
                    int x = stack[j + 0];
                    int y = stack[j + 1];
                    int i = stack[j + 2];
                    if (i < 0)
                    {
                        failed++;
                        continue;
                    }

                    int r = srcReg[i];
                    int d2 = 0xffff;
                    int area = chf.areas[i];
                    RcCompactSpan s = chf.spans[i];
                    for (int dir = 0; dir < 4; ++dir)
                    {
                        if (GetCon(s, dir) == RC_NOT_CONNECTED)
                        {
                            continue;
                        }

                        int ax = x + GetDirOffsetX(dir);
                        int ay = y + GetDirOffsetY(dir);
                        int ai = chf.cells[ax + ay * w].index + GetCon(s, dir);
                        if (chf.areas[ai] != area)
                        {
                            continue;
                        }

                        if (srcReg[ai] > 0 && (srcReg[ai] & RC_BORDER_REG) == 0)
                        {
                            if (srcDist[ai] + 2 < d2)
                            {
                                r = srcReg[ai];
                                d2 = srcDist[ai] + 2;
                            }
                        }
                    }

                    if (r != 0)
                    {
                        stack[j + 2] = -1; // mark as used       // 标记为已使用
                        dirtyEntries.Add(i);
                        dirtyEntries.Add(r);
                        dirtyEntries.Add(d2);
                    }
                    else
                    {
                        failed++;
                    }
                }

                // Copy entries that differ between src and dst to keep them in sync.
                // 复制 src 和 dst 之间不同的条目以保持它们同步。
                for (int i = 0; i < dirtyEntries.Count; i += 3)
                {
                    int idx = dirtyEntries[i];
                    srcReg[idx] = dirtyEntries[i + 1];
                    srcDist[idx] = dirtyEntries[i + 2];
                }

                if (failed * 3 == stack.Count())
                {
                    break;
                }

                if (level > 0)
                {
                    ++iter;
                    if (iter >= maxIter)
                    {
                        break;
                    }
                }
            }

            return srcReg;
        }

        // 根据给定的起始级别、紧凑高度场（RcCompactHeightfield）和源区域数组，将单元格按级别排序到堆栈中。
        // 方法首先清空所有堆栈，然后遍历高度场中的每个跨度，将它们分配到适当的堆栈中，基于它们的级别范围。
        private static void SortCellsByLevel(int startLevel, RcCompactHeightfield chf, int[] srcReg, int nbStacks,
            List<List<int>> stacks, int loglevelsPerStack) // the levels per stack (2 in our case) as a bit shift
        {
            int w = chf.width;
            int h = chf.height;
            startLevel = startLevel >> loglevelsPerStack;

            for (int j = 0; j < nbStacks; ++j)
            {
                stacks[j].Clear();
            }

            // put all cells in the level range into the appropriate stacks
            // 将级别范围内的所有单元格放入适当的堆栈中
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        if (chf.areas[i] == RC_NULL_AREA || srcReg[i] != 0)
                        {
                            continue;
                        }

                        int level = chf.dist[i] >> loglevelsPerStack;
                        int sId = startLevel - level;
                        if (sId >= nbStacks)
                        {
                            continue;
                        }

                        if (sId < 0)
                        {
                            sId = 0;
                        }

                        stacks[sId].Add(x);
                        stacks[sId].Add(y);
                        stacks[sId].Add(i);
                    }
                }
            }
        }

        // 将源堆栈中的元素附加到目标堆栈中。方法遍历源堆栈中的每个元素，检查其是否有效且尚未分配区域ID，然后将其添加到目标堆栈中。
        private static void AppendStacks(List<int> srcStack, List<int> dstStack, int[] srcReg)
        {
            for (int j = 0; j < srcStack.Count; j += 3)
            {
                int i = srcStack[j + 2];
                if ((i < 0) || (srcReg[i] != 0))
                {
                    continue;
                }

                dstStack.Add(srcStack[j]);
                dstStack.Add(srcStack[j + 1]);
                dstStack.Add(srcStack[j + 2]);
            }
        }
        
        // 从给定的区域（RcRegion）中删除相邻的重复连接。方法遍历区域的连接列表，如果找到相邻的重复连接，则将其从列表中删除。
        private static void RemoveAdjacentNeighbours(RcRegion reg)
        {
            // Remove adjacent duplicates.
            // 删除相邻的重复项。
            for (int i = 0; i < reg.connections.Count && reg.connections.Count > 1;)
            {
                int ni = (i + 1) % reg.connections.Count;
                if (reg.connections[i] == reg.connections[ni])
                {
                    reg.connections.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }

        // 在给定的区域（RcRegion）中将一个邻居ID替换为另一个邻居ID。方法首先遍历区域的连接列表，将旧的邻居ID替换为新的邻居ID。
        // 然后遍历区域的楼层列表，执行相同的替换操作。如果邻居已更改，则调用RemoveAdjacentNeighbours方法以删除可能的重复连接。
        private static void ReplaceNeighbour(RcRegion reg, int oldId, int newId)
        {
            bool neiChanged = false;
            for (int i = 0; i < reg.connections.Count; ++i)
            {
                if (reg.connections[i] == oldId)
                {
                    reg.connections[i] = newId;
                    neiChanged = true;
                }
            }

            for (int i = 0; i < reg.floors.Count; ++i)
            {
                if (reg.floors[i] == oldId)
                {
                    reg.floors[i] = newId;
                }
            }

            if (neiChanged)
            {
                RemoveAdjacentNeighbours(reg);
            }
        }

        // CanMergeWithRegion：检查两个给定区域（RcRegion）是否可以合并。方法首先检查两个区域的区域类型是否相同。
        // 然后计算两个区域之间的连接数量。如果连接数量大于1，则不允许合并。
        // 最后，检查区域A的楼层列表中是否包含区域B的ID。如果包含，则不允许合并。否则，允许合并。
        private static bool CanMergeWithRegion(RcRegion rega, RcRegion regb)
        {
            if (rega.areaType != regb.areaType)
            {
                return false;
            }

            int n = 0;
            for (int i = 0; i < rega.connections.Count; ++i)
            {
                if (rega.connections[i] == regb.id)
                {
                    n++;
                }
            }

            if (n > 1)
            {
                return false;
            }

            for (int i = 0; i < rega.floors.Count; ++i)
            {
                if (rega.floors[i] == regb.id)
                {
                    return false;
                }
            }

            return true;
        }

        // 将一个唯一的楼层区域ID添加到给定的区域（RcRegion）。如果区域的楼层列表中尚未包含该ID，则将其添加到列表中。
        private static void AddUniqueFloorRegion(RcRegion reg, int n)
        {
            if (!reg.floors.Contains(n))
            {
                reg.floors.Add(n);
            }
        }

        // 合并两个给定的区域（RcRegion）。方法首先找到两个区域的插入点，然后合并它们的连接列表，并调用RemoveAdjacentNeighbours方法删除可能的重复连接。
        // 接下来，将区域B的楼层列表中的所有ID添加到区域A的楼层列表中，最后将区域B的跨度计数设置为0并清空其连接列表。返回合并是否成功。
        private static bool MergeRegions(RcRegion rega, RcRegion regb)
        {
            int aid = rega.id;
            int bid = regb.id;

            // Duplicate current neighbourhood.
            // 复制当前邻居。
            List<int> acon = new List<int>(rega.connections);
            List<int> bcon = regb.connections;

            // Find insertion point on A.
            // 找到 A 上的插入点。
            int insa = -1;
            for (int i = 0; i < acon.Count; ++i)
            {
                if (acon[i] == bid)
                {
                    insa = i;
                    break;
                }
            }

            if (insa == -1)
            {
                return false;
            }

            // Find insertion point on B.
            // 找到 B 上的插入点。
            int insb = -1;
            for (int i = 0; i < bcon.Count; ++i)
            {
                if (bcon[i] == aid)
                {
                    insb = i;
                    break;
                }
            }

            if (insb == -1)
            {
                return false;
            }

            // Merge neighbours.
            // 合并邻居。
            rega.connections.Clear();
            for (int i = 0, ni = acon.Count; i < ni - 1; ++i)
            {
                rega.connections.Add(acon[(insa + 1 + i) % ni]);
            }

            for (int i = 0, ni = bcon.Count; i < ni - 1; ++i)
            {
                rega.connections.Add(bcon[(insb + 1 + i) % ni]);
            }

            RemoveAdjacentNeighbours(rega);

            for (int j = 0; j < regb.floors.Count; ++j)
            {
                AddUniqueFloorRegion(rega, regb.floors[j]);
            }

            rega.spanCount += regb.spanCount;
            regb.spanCount = 0;
            regb.connections.Clear();

            return true;
        }

        // 检查给定的区域（RcRegion）是否连接到边界。如果区域的连接列表中包含0（表示边界），则认为该区域连接到边界。
        private static bool IsRegionConnectedToBorder(RcRegion reg)
        {
            // Region is connected to border if one of the neighbours is null id.
            // 如果邻居之一为空 id，则区域连接到边界。
            return reg.connections.Contains(0);
        }

        // 检查给定的紧凑高度场（RcCompactHeightfield）、源区域数组、坐标（x，y）、跨度索引和方向是否构成一个固体边缘。
        // 方法首先获取给定方向上的邻居跨度的区域ID，然后检查它是否与当前跨度的区域ID相同。如果相同，则返回false，表示不是固体边缘；否则返回true。
        private static bool IsSolidEdge(RcCompactHeightfield chf, int[] srcReg, int x, int y, int i, int dir)
        {
            RcCompactSpan s = chf.spans[i];
            int r = 0;
            if (GetCon(s, dir) != RC_NOT_CONNECTED)
            {
                int ax = x + GetDirOffsetX(dir);
                int ay = y + GetDirOffsetY(dir);
                int ai = chf.cells[ax + ay * chf.width].index + GetCon(s, dir);
                r = srcReg[ai];
            }

            if (r == srcReg[i])
            {
                return false;
            }

            return true;
        }

        // 沿着给定的紧凑高度场（RcCompactHeightfield）的边缘行走，以找到区域之间的连接。
        // 方法首先设置起始点和当前区域ID，然后遍历边缘，检查每个点是否构成一个固体边缘。如果找到一个固体边缘，将其区域ID添加到连接列表中，然后沿顺时针方向旋转。
        // 否则，将当前点移动到下一个邻居跨度，并沿逆时针方向旋转。最后，删除连接列表中的相邻重复项。
        private static void WalkContour(int x, int y, int i, int dir, RcCompactHeightfield chf, int[] srcReg,
            List<int> cont)
        {
            int startDir = dir;
            int starti = i;

            RcCompactSpan ss = chf.spans[i];
            int curReg = 0;
            if (GetCon(ss, dir) != RC_NOT_CONNECTED)
            {
                int ax = x + GetDirOffsetX(dir);
                int ay = y + GetDirOffsetY(dir);
                int ai = chf.cells[ax + ay * chf.width].index + GetCon(ss, dir);
                curReg = srcReg[ai];
            }

            cont.Add(curReg);

            int iter = 0;
            while (++iter < 40000)
            {
                RcCompactSpan s = chf.spans[i];

                if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                {
                    // Choose the edge corner
                    // 选择边角
                    int r = 0;
                    if (GetCon(s, dir) != RC_NOT_CONNECTED)
                    {
                        int ax = x + GetDirOffsetX(dir);
                        int ay = y + GetDirOffsetY(dir);
                        int ai = chf.cells[ax + ay * chf.width].index + GetCon(s, dir);
                        r = srcReg[ai];
                    }

                    if (r != curReg)
                    {
                        curReg = r;
                        cont.Add(curReg);
                    }

                    dir = (dir + 1) & 0x3; // Rotate CW      // 顺时针旋转
                }
                else
                {
                    int ni = -1;
                    int nx = x + GetDirOffsetX(dir);
                    int ny = y + GetDirOffsetY(dir);
                    if (GetCon(s, dir) != RC_NOT_CONNECTED)
                    {
                        RcCompactCell nc = chf.cells[nx + ny * chf.width];
                        ni = nc.index + GetCon(s, dir);
                    }

                    if (ni == -1)
                    {
                        // Should not happen.        // 不应该发生。
                        return;
                    }

                    x = nx;
                    y = ny;
                    i = ni;
                    dir = (dir + 3) & 0x3; // Rotate CCW        // 逆时针旋转
                }

                if (starti == i && startDir == dir)
                {
                    break;
                }
            }

            // Remove adjacent duplicates.     // 删除相邻的重复项。
            if (cont.Count > 1)
            {
                for (int j = 0; j < cont.Count;)
                {
                    int nj = (j + 1) % cont.Count;
                    if (cont[j] == cont[nj])
                    {
                        cont.RemoveAt(j);
                    }
                    else
                    {
                        ++j;
                    }
                }
            }
        }

        // 合并和过滤区域。这个方法首先构造区域并找到区域的边缘以及周围的连接。接下来，它会移除太小的区域，并将太小的区域合并到相邻区域中。
        // 然后，它会压缩区域ID并将重叠区域的ID添加到overlaps列表中。最后，返回最大区域ID。
        private static int MergeAndFilterRegions(RcTelemetry ctx, int minRegionArea, int mergeRegionSize, int maxRegionId,
            RcCompactHeightfield chf, int[] srcReg, List<int> overlaps)
        {
            int w = chf.width;
            int h = chf.height;

            int nreg = maxRegionId + 1;
            RcRegion[] regions = new RcRegion[nreg];

            // Construct regions      // 构造区域
            for (int i = 0; i < nreg; ++i)
            {
                regions[i] = new RcRegion(i);
            }

            // Find edge of a region and find connections around the contour.
            // 查找区域的边缘并查找轮廓周围的连接。
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        int r = srcReg[i];
                        if (r == 0 || r >= nreg)
                        {
                            continue;
                        }

                        RcRegion reg = regions[r];
                        reg.spanCount++;

                        // Update floors.     // 更新楼层。
                        for (int j = c.index; j < ni; ++j)
                        {
                            if (i == j)
                            {
                                continue;
                            }

                            int floorId = srcReg[j];
                            if (floorId == 0 || floorId >= nreg)
                            {
                                continue;
                            }

                            if (floorId == r)
                            {
                                reg.overlap = true;
                            }

                            AddUniqueFloorRegion(reg, floorId);
                        }

                        // Have found contour      // 找到轮廓
                        if (reg.connections.Count > 0)
                        {
                            continue;
                        }

                        reg.areaType = chf.areas[i];

                        // Check if this cell is next to a border.
                        // 检查该单元格是否位于边框旁边。
                        int ndir = -1;
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                            {
                                ndir = dir;
                                break;
                            }
                        }

                        if (ndir != -1)
                        {
                            // The cell is at border.
                            // Walk around the contour to find all the neighbours.
                            // 单元格位于边界。
                            // 绕着轮廓走，找到所有的邻居。
                            WalkContour(x, y, i, ndir, chf, srcReg, reg.connections);
                        }
                    }
                }
            }

            // Remove too small regions.
            // 删除太小的区域。
            List<int> stack = new List<int>(32);
            List<int> trace = new List<int>(32);
            for (int i = 0; i < nreg; ++i)
            {
                RcRegion reg = regions[i];
                if (reg.id == 0 || (reg.id & RC_BORDER_REG) != 0)
                {
                    continue;
                }

                if (reg.spanCount == 0)
                {
                    continue;
                }

                if (reg.visited)
                {
                    continue;
                }

                // Count the total size of all the connected regions.
                // Also keep track of the regions connects to a tile border.
                // 计算所有连接区域的总大小。
                // 还要跟踪连接到图块边框的区域。
                bool connectsToBorder = false;
                int spanCount = 0;
                stack.Clear();
                trace.Clear();

                reg.visited = true;
                stack.Add(i);

                while (stack.Count > 0)
                {
                    // Pop
                    int ri = stack[^1];
                    stack.RemoveAt(stack.Count - 1);

                    RcRegion creg = regions[ri];

                    spanCount += creg.spanCount;
                    trace.Add(ri);

                    for (int j = 0; j < creg.connections.Count; ++j)
                    {
                        if ((creg.connections[j] & RC_BORDER_REG) != 0)
                        {
                            connectsToBorder = true;
                            continue;
                        }

                        RcRegion neireg = regions[creg.connections[j]];
                        if (neireg.visited)
                        {
                            continue;
                        }

                        if (neireg.id == 0 || (neireg.id & RC_BORDER_REG) != 0)
                        {
                            continue;
                        }

                        // Visit
                        stack.Add(neireg.id);
                        neireg.visited = true;
                    }
                }

                // If the accumulated regions size is too small, remove it.
                // Do not remove areas which connect to tile borders as their size cannot be estimated correctly and removing them can potentially remove necessary areas.
                // 如果累积的区域大小太小，则将其删除。
                // 不要删除连接到图块边框的区域，因为它们的大小无法正确估计，并且删除它们可能会删除必要的区域。
                if (spanCount < minRegionArea && !connectsToBorder)
                {
                    // Kill all visited regions.
                    // 杀死所有访问过的区域。
                    for (int j = 0; j < trace.Count; ++j)
                    {
                        regions[trace[j]].spanCount = 0;
                        regions[trace[j]].id = 0;
                    }
                }
            }

            // Merge too small regions to neighbour regions.
            // 将太小的区域合并到相邻区域。
            int mergeCount = 0;
            do
            {
                mergeCount = 0;
                for (int i = 0; i < nreg; ++i)
                {
                    RcRegion reg = regions[i];
                    if (reg.id == 0 || (reg.id & RC_BORDER_REG) != 0)
                    {
                        continue;
                    }

                    if (reg.overlap)
                    {
                        continue;
                    }

                    if (reg.spanCount == 0)
                    {
                        continue;
                    }

                    // Check to see if the region should be merged.
                    // 检查该区域是否应该合并。
                    if (reg.spanCount > mergeRegionSize && IsRegionConnectedToBorder(reg))
                    {
                        continue;
                    }

                    // Small region with more than 1 connection.
                    // Or region which is not connected to a border at all.
                    // Find smallest neighbour region that connects to this one.
                    // 具有超过 1 个连接的小区域。
                    // 或者根本不连接到边界的区域。
                    // 找到连接到该区域的最小邻居区域。
                    int smallest = 0xfffffff;
                    int mergeId = reg.id;
                    for (int j = 0; j < reg.connections.Count; ++j)
                    {
                        if ((reg.connections[j] & RC_BORDER_REG) != 0)
                        {
                            continue;
                        }

                        RcRegion mreg = regions[reg.connections[j]];
                        if (mreg.id == 0 || (mreg.id & RC_BORDER_REG) != 0 || mreg.overlap)
                        {
                            continue;
                        }

                        if (mreg.spanCount < smallest && CanMergeWithRegion(reg, mreg) && CanMergeWithRegion(mreg, reg))
                        {
                            smallest = mreg.spanCount;
                            mergeId = mreg.id;
                        }
                    }

                    // Found new id.     // 找到新的 id。
                    if (mergeId != reg.id)
                    {
                        int oldId = reg.id;
                        RcRegion target = regions[mergeId];

                        // Merge neighbours.      // 合并邻居。
                        if (MergeRegions(target, reg))
                        {
                            // Fixup regions pointing to current region.      // 指向当前区域的修复区域。
                            for (int j = 0; j < nreg; ++j)
                            {
                                if (regions[j].id == 0 || (regions[j].id & RC_BORDER_REG) != 0)
                                {
                                    continue;
                                }

                                // If another region was already merged into current region
                                // change the nid of the previous region too.
                                // 如果另一个区域已经合并到当前区域，也更改前一个区域的 nid。
                                if (regions[j].id == oldId)
                                {
                                    regions[j].id = mergeId;
                                }

                                // Replace the current region with the new one if the
                                // current regions is neighbour.
                                // 如果当前区域是相邻区域，则将当前区域替换为新区域。
                                ReplaceNeighbour(regions[j], oldId, mergeId);
                            }

                            mergeCount++;
                        }
                    }
                }
            } while (mergeCount > 0);

            // Compress region Ids.
            // 压缩区域 ID。
            for (int i = 0; i < nreg; ++i)
            {
                regions[i].remap = false;
                if (regions[i].id == 0)
                {
                    continue; // Skip nil regions.      // 跳过零区域。
                }

                if ((regions[i].id & RC_BORDER_REG) != 0)
                {
                    continue; // Skip external regions.   // 跳过外部区域。
                }

                regions[i].remap = true;
            }

            int regIdGen = 0;
            for (int i = 0; i < nreg; ++i)
            {
                if (!regions[i].remap)
                {
                    continue;
                }

                int oldId = regions[i].id;
                int newId = ++regIdGen;
                for (int j = i; j < nreg; ++j)
                {
                    if (regions[j].id == oldId)
                    {
                        regions[j].id = newId;
                        regions[j].remap = false;
                    }
                }
            }

            maxRegionId = regIdGen;

            // Remap regions.            // 重新映射区域。
            for (int i = 0; i < chf.spanCount; ++i)
            {
                if ((srcReg[i] & RC_BORDER_REG) == 0)
                {
                    srcReg[i] = regions[srcReg[i]].id;
                }
            }

            // Return regions that we found to be overlapping.         // 返回我们发现重叠的区域。
            for (int i = 0; i < nreg; ++i)
            {
                if (regions[i].overlap)
                {
                    overlaps.Add(regions[i].id);
                }
            }

            return maxRegionId;
        }

        //这是一个私有的辅助方法，用于将一个区域连接到另一个区域，确保连接是唯一的。
        //如果区域reg尚未连接到区域n，则将区域n添加到区域reg的连接列表中。
        private static void AddUniqueConnection(RcRegion reg, int n)
        {
            if (!reg.connections.Contains(n))
            {
                reg.connections.Add(n);
            }
        }

        // 此方法用于合并和过滤层次化区域。它首先构建区域，然后找到相邻的区域和重叠的区域。
        // 接下来，它创建一个2D层次结构，并合并单调区域以创建非重叠区域。最后，它删除小区域并压缩区域ID。
        private static int MergeAndFilterLayerRegions(RcTelemetry ctx, int minRegionArea, int maxRegionId,
            RcCompactHeightfield chf, int[] srcReg, List<int> overlaps)
        {
            int w = chf.width;
            int h = chf.height;

            int nreg = maxRegionId + 1;
            RcRegion[] regions = new RcRegion[nreg];

            // Construct regions        // 构造区域
            for (int i = 0; i < nreg; ++i)
            {
                regions[i] = new RcRegion(i);
            }

            // Find region neighbours and overlapping regions.
            // 查找区域邻居和重叠区域。
            List<int> lregs = new List<int>(32);
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
                        if (ri == 0 || ri >= nreg)
                        {
                            continue;
                        }

                        RcRegion reg = regions[ri];

                        reg.spanCount++;
                        reg.areaType = chf.areas[i];
                        reg.ymin = Math.Min(reg.ymin, s.y);
                        reg.ymax = Math.Max(reg.ymax, s.y);
                        // Collect all region layers.           // 收集所有区域图层。
                        lregs.Add(ri);

                        // Update neighbours         // 更新邻居
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RC_NOT_CONNECTED)
                            {
                                int ax = x + GetDirOffsetX(dir);
                                int ay = y + GetDirOffsetY(dir);
                                int ai = chf.cells[ax + ay * w].index + GetCon(s, dir);
                                int rai = srcReg[ai];
                                if (rai > 0 && rai < nreg && rai != ri)
                                {
                                    AddUniqueConnection(reg, rai);
                                }

                                if ((rai & RC_BORDER_REG) != 0)
                                {
                                    reg.connectsToBorder = true;
                                }
                            }
                        }
                    }

                    // Update overlapping regions.         // 更新重叠区域。
                    for (int i = 0; i < lregs.Count - 1; ++i)
                    {
                        for (int j = i + 1; j < lregs.Count; ++j)
                        {
                            if (lregs[i] != lregs[j])
                            {
                                RcRegion ri = regions[lregs[i]];
                                RcRegion rj = regions[lregs[j]];
                                AddUniqueFloorRegion(ri, lregs[j]);
                                AddUniqueFloorRegion(rj, lregs[i]);
                            }
                        }
                    }
                }
            }

            // Create 2D layers from regions.      // 从区域创建 2D 图层。
            int layerId = 1;

            for (int i = 0; i < nreg; ++i)
            {
                regions[i].id = 0;
            }

            // Merge montone regions to create non-overlapping areas.       // 合并单色调区域以创建不重叠的区域。
            List<int> stack = new List<int>(32);
            for (int i = 1; i < nreg; ++i)
            {
                RcRegion root = regions[i];
                // Skip already visited.             // 跳过已经访问过的。
                if (root.id != 0)
                {
                    continue;
                }

                // Start search.       // 开始寻找。
                root.id = layerId;

                stack.Clear();
                stack.Add(i);

                while (stack.Count > 0)
                {
                    // Pop front
                    var idx = stack[0];
                    stack.RemoveAt(0);
                    RcRegion reg = regions[idx];

                    int ncons = reg.connections.Count;
                    for (int j = 0; j < ncons; ++j)
                    {
                        int nei = reg.connections[j];
                        RcRegion regn = regions[nei];
                        // Skip already visited.      // 跳过已经访问过的。
                        if (regn.id != 0)
                        {
                            continue;
                        }

                        // Skip if different area type, do not connect regions with different area type.
                        // 如果区域类型不同则跳过，不连接不同区域类型的区域。
                        if (reg.areaType != regn.areaType)
                        {
                            continue;
                        }

                        // Skip if the neighbour is overlapping root region.
                        // 如果邻居与根区域重叠则跳过。
                        bool overlap = false;
                        for (int k = 0; k < root.floors.Count; k++)
                        {
                            if (root.floors[k] == nei)
                            {
                                overlap = true;
                                break;
                            }
                        }

                        if (overlap)
                        {
                            continue;
                        }

                        // Deepen
                        stack.Add(nei);

                        // Mark layer id        // 标记图层id
                        regn.id = layerId;
                        // Merge current layers to root.  // 将当前层合并到根。
                        for (int k = 0; k < regn.floors.Count; ++k)
                        {
                            AddUniqueFloorRegion(root, regn.floors[k]);
                        }

                        root.ymin = Math.Min(root.ymin, regn.ymin);
                        root.ymax = Math.Max(root.ymax, regn.ymax);
                        root.spanCount += regn.spanCount;
                        regn.spanCount = 0;
                        root.connectsToBorder = root.connectsToBorder || regn.connectsToBorder;
                    }
                }

                layerId++;
            }

            // Remove small regions          // 删除小区域
            for (int i = 0; i < nreg; ++i)
            {
                if (regions[i].spanCount > 0 && regions[i].spanCount < minRegionArea && !regions[i].connectsToBorder)
                {
                    int reg = regions[i].id;
                    for (int j = 0; j < nreg; ++j)
                    {
                        if (regions[j].id == reg)
                        {
                            regions[j].id = 0;
                        }
                    }
                }
            }

            // Compress region Ids.        // 压缩区域 ID。
            for (int i = 0; i < nreg; ++i)
            {
                regions[i].remap = false;
                if (regions[i].id == 0)
                {
                    continue; // Skip nil regions.       // 跳过零区域。
                }

                if ((regions[i].id & RC_BORDER_REG) != 0)
                {
                    continue; // Skip external regions.   // 跳过外部区域。
                }

                regions[i].remap = true;
            }

            int regIdGen = 0;
            for (int i = 0; i < nreg; ++i)
            {
                if (!regions[i].remap)
                {
                    continue;
                }

                int oldId = regions[i].id;
                int newId = ++regIdGen;
                for (int j = i; j < nreg; ++j)
                {
                    if (regions[j].id == oldId)
                    {
                        regions[j].id = newId;
                        regions[j].remap = false;
                    }
                }
            }

            maxRegionId = regIdGen;

            // Remap regions.       // 重新映射区域。
            for (int i = 0; i < chf.spanCount; ++i)
            {
                if ((srcReg[i] & RC_BORDER_REG) == 0)
                {
                    srcReg[i] = regions[srcReg[i]].id;
                }
            }

            return maxRegionId;
        }

        /// @par
        ///
        /// This is usually the second to the last step in creating a fully built
        /// compact heightfield. This step is required before regions are built
        /// using #rcBuildRegions or #rcBuildRegionsMonotone.
        /// 这通常是创建完整构建的第二步到最后一步紧凑的高度场。 在使用 #rcBuildRegions 或 #rcBuildRegionsMonotone 构建区域之前需要执行此步骤。
        ///
        /// After this step, the distance data is available via the rcCompactHeightfield::maxDistance
        /// and rcCompactHeightfield::dist fields.
        /// 在此步骤之后，可通过 rcCompactHeightfield::maxDistance 和 rcCompactHeightfield::dist 字段获取距离数据。
        ///
        /// @see rcCompactHeightfield, rcBuildRegions, rcBuildRegionsMonotone
        // 此方法用于构建距离场。距离场是一个在每个单元格中存储与障碍物的距离的网格。这是在构建区域之前计算的必要步骤。
        public static void BuildDistanceField(RcTelemetry ctx, RcCompactHeightfield chf)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_BUILD_DISTANCEFIELD);

            int[] src = new int[chf.spanCount];

            ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_DISTANCEFIELD_DIST);
            int maxDist = CalculateDistanceField(chf, src);
            chf.maxDistance = maxDist;
            ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_DISTANCEFIELD_DIST);

            ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_DISTANCEFIELD_BLUR);

            // Blur       // 模糊
            src = BoxBlur(chf, 1, src);

            // Store distance.     // 存储距离。
            chf.dist = src;

            ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_DISTANCEFIELD_BLUR);
        }

        // 此方法用于在给定的矩形区域内为区域分配一个特定的ID。它遍历矩形区域内的所有单元格，并将其分配给指定的区域ID。
        private static void PaintRectRegion(int minx, int maxx, int miny, int maxy, int regId, RcCompactHeightfield chf,
            int[] srcReg)
        {
            int w = chf.width;
            for (int y = miny; y < maxy; ++y)
            {
                for (int x = minx; x < maxx; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];
                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        if (chf.areas[i] != RC_NULL_AREA)
                        {
                            srcReg[i] = regId;
                        }
                    }
                }
            }
        }

        /// @par
        ///
        /// Non-null regions will consist of connected, non-overlapping walkable spans that form a single contour.
        /// Contours will form simple polygons.
        /// 非空区域将由连接的、不重叠的可步行跨度组成，形成单个轮廓。 轮廓将形成简单的多边形。
        ///
        /// If multiple regions form an area that is smaller than @p minRegionArea, then all spans will be
        /// re-assigned to the zero (null) region.
        /// 非空区域将由连接的、不重叠的可步行跨度组成，形成单个轮廓。 轮廓将形成简单的多边形。
        ///
        /// Partitioning can result in smaller than necessary regions. @p mergeRegionArea helps
        /// reduce unnecessarily small regions.
        /// 分区可能会导致区域小于所需区域。 @p mergeRegionArea 有助于减少不必要的小区域。
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.
        /// 有关配置参数的更多信息，请参阅#rcConfig 文档。
        ///
        /// The region data will be available via the rcCompactHeightfield::maxRegions
        /// and rcCompactSpan::reg fields.
        /// 区域数据将通过 rcCompactHeightfield::maxRegions 和 rcCompactSpan::reg 字段提供。
        ///
        /// @warning The distance field must be created using #rcBuildDistanceField before attempting to build regions.
        /// @warning 在尝试构建区域之前，必须使用#rcBuildDistanceField 创建距离字段。
        ///
        /// @see rcCompactHeightfield, rcCompactSpan, rcBuildDistanceField, rcBuildRegionsMonotone, rcConfig
        //此方法用于构建单调区域。单调区域是一种简化的区域表示，它们是连续的、不重叠的可行走跨度，形成一个简单的多边形。
        //这个方法首先标记边界区域，然后逐行扫描紧凑高度场中的跨度，将它们分配给一个区域。接下来，它合并相邻的区域并创建唯一的区域ID。
        public static void BuildRegionsMonotone(RcTelemetry ctx, RcCompactHeightfield chf, int minRegionArea,
            int mergeRegionArea)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS);

            int w = chf.width;
            int h = chf.height;
            int borderSize = chf.borderSize;
            int id = 1;

            int[] srcReg = new int[chf.spanCount];

            int nsweeps = Math.Max(chf.width, chf.height);
            RcSweepSpan[] sweeps = new RcSweepSpan[nsweeps];
            for (int i = 0; i < sweeps.Length; i++)
            {
                sweeps[i] = new RcSweepSpan();
            }

            // Mark border regions.        // 标记边界区域。
            if (borderSize > 0)
            {
                // Make sure border will not overflow.
                // 确保边框不会溢出。
                int bw = Math.Min(w, borderSize);
                int bh = Math.Min(h, borderSize);
                // Paint regions
                // 绘制区域
                PaintRectRegion(0, bw, 0, h, id | RC_BORDER_REG, chf, srcReg);
                id++;
                PaintRectRegion(w - bw, w, 0, h, id | RC_BORDER_REG, chf, srcReg);
                id++;
                PaintRectRegion(0, w, 0, bh, id | RC_BORDER_REG, chf, srcReg);
                id++;
                PaintRectRegion(0, w, h - bh, h, id | RC_BORDER_REG, chf, srcReg);
                id++;
            }

            int[] prev = new int[1024];

            // Sweep one line at a time.
            // 一次扫描一行。
            for (int y = borderSize; y < h - borderSize; ++y)
            {
                // Collect spans from this row.
                // 收集该行的跨度。
                if (prev.Length < id * 2)
                {
                    prev = new int[id * 2];
                }
                else
                {
                    Array.Fill(prev, 0, 0, (id) - (0));
                }

                int rid = 1;

                for (int x = borderSize; x < w - borderSize; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];

                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];
                        if (chf.areas[i] == RC_NULL_AREA)
                        {
                            continue;
                        }

                        // -x
                        int previd = 0;
                        if (GetCon(s, 0) != RC_NOT_CONNECTED)
                        {
                            int ax = x + GetDirOffsetX(0);
                            int ay = y + GetDirOffsetY(0);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 0);
                            if ((srcReg[ai] & RC_BORDER_REG) == 0 && chf.areas[i] == chf.areas[ai])
                            {
                                previd = srcReg[ai];
                            }
                        }

                        if (previd == 0)
                        {
                            previd = rid++;
                            sweeps[previd].rid = previd;
                            sweeps[previd].ns = 0;
                            sweeps[previd].nei = 0;
                        }

                        // -y
                        if (GetCon(s, 3) != RC_NOT_CONNECTED)
                        {
                            int ax = x + GetDirOffsetX(3);
                            int ay = y + GetDirOffsetY(3);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 3);
                            if (srcReg[ai] != 0 && (srcReg[ai] & RC_BORDER_REG) == 0 && chf.areas[i] == chf.areas[ai])
                            {
                                int nr = srcReg[ai];
                                if (sweeps[previd].nei == 0 || sweeps[previd].nei == nr)
                                {
                                    sweeps[previd].nei = nr;
                                    sweeps[previd].ns++;
                                    if (prev.Length <= nr)
                                    {
                                        Array.Resize(ref prev, prev.Length * 2);
                                    }

                                    prev[nr]++;
                                }
                                else
                                {
                                    sweeps[previd].nei = RC_NULL_NEI;
                                }
                            }
                        }

                        srcReg[i] = previd;
                    }
                }

                // Create unique ID.
                // 创建唯一 ID。
                for (int i = 1; i < rid; ++i)
                {
                    if (sweeps[i].nei != RC_NULL_NEI && sweeps[i].nei != 0 && prev[sweeps[i].nei] == sweeps[i].ns)
                    {
                        sweeps[i].id = sweeps[i].nei;
                    }
                    else
                    {
                        sweeps[i].id = id++;
                    }
                }

                // Remap IDs
                // 重新映射 ID
                for (int x = borderSize; x < w - borderSize; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];

                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        if (srcReg[i] > 0 && srcReg[i] < rid)
                        {
                            srcReg[i] = sweeps[srcReg[i]].id;
                        }
                    }
                }
            }

            ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FILTER);
            // Merge regions and filter out small regions.
            // 合并区域并过滤掉小区域。
            List<int> overlaps = new List<int>();
            chf.maxRegions = MergeAndFilterRegions(ctx, minRegionArea, mergeRegionArea, id, chf, srcReg, overlaps);

            // Monotone partitioning does not generate overlapping regions.
            // 单调分区不会生成重叠区域。
            ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FILTER);

            // Store the result out.
            // 将结果存储出来。
            for (int i = 0; i < chf.spanCount; ++i)
            {
                chf.spans[i].reg = srcReg[i];
            }
        }

        /// @par
        ///
        /// Non-null regions will consist of connected, non-overlapping walkable spans that form a single contour.
        /// Contours will form simple polygons.
        /// 非空区域将由连接的、不重叠的可步行跨度组成，形成单个轮廓。 轮廓将形成简单的多边形。
        ///
        /// If multiple regions form an area that is smaller than @p minRegionArea, then all spans will be
        /// re-assigned to the zero (null) region.
        /// 如果多个区域形成一个小于@p minRegionArea 的区域，则所有跨度将被重新分配给零（null）区域。
        ///
        /// Watershed partitioning can result in smaller than necessary regions, especially in diagonal corridors.
        /// @p mergeRegionArea helps reduce unnecessarily small regions.
        /// 如果多个区域形成一个小于@p minRegionArea 的区域，则所有跨度将被重新分配给零（null）区域。
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.
        /// 有关配置参数的更多信息，请参阅#rcConfig 文档。
        ///
        /// The region data will be available via the rcCompactHeightfield::maxRegions
        /// and rcCompactSpan::reg fields.
        /// 区域数据将通过 rcCompactHeightfield::maxRegions 和 rcCompactSpan::reg 字段提供。
        ///
        /// @warning The distance field must be created using #rcBuildDistanceField before attempting to build regions.
        ///  @warning 在尝试构建区域之前，必须使用#rcBuildDistanceField 创建距离字段。
        ///
        /// @see rcCompactHeightfield, rcCompactSpan, rcBuildDistanceField, rcBuildRegionsMonotone, rcConfig
        // 此方法用于构建区域，它是一个更通用的方法，可以处理多种类型的区域。
        // 它首先计算距离场，然后根据距离场创建区域。最后，它合并区域并过滤掉小区域。
        public static void BuildRegions(RcTelemetry ctx, RcCompactHeightfield chf, int minRegionArea,
            int mergeRegionArea)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS);

            int w = chf.width;
            int h = chf.height;
            int borderSize = chf.borderSize;

            ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_WATERSHED);

            int LOG_NB_STACKS = 3;
            int NB_STACKS = 1 << LOG_NB_STACKS;
            List<List<int>> lvlStacks = new List<List<int>>();
            for (int i = 0; i < NB_STACKS; ++i)
            {
                lvlStacks.Add(new List<int>(1024));
            }

            List<int> stack = new List<int>(1024);

            int[] srcReg = new int[chf.spanCount];
            int[] srcDist = new int[chf.spanCount];

            int regionId = 1;
            int level = (chf.maxDistance + 1) & ~1;

            // TODO: Figure better formula, expandIters defines how much the
            // watershed "overflows" and simplifies the regions. Tying it to
            // agent radius was usually good indication how greedy it could be.
            // readonly int expandIters = 4 + walkableRadius * 2;
            // TODO：绘制更好的公式，expandIters 定义分水岭“溢出”的程度并简化区域。 将它与代理半径联系起来通常可以很好地表明它的贪婪程度。 只读 int ExpandIters = 4 + walkableRadius * 2;
            int expandIters = 8;

            if (borderSize > 0)
            {
                // Make sure border will not overflow.
                // 确保边框不会溢出。
                int bw = Math.Min(w, borderSize);
                int bh = Math.Min(h, borderSize);
                // Paint regions
                // 绘制区域
                PaintRectRegion(0, bw, 0, h, regionId | RC_BORDER_REG, chf, srcReg);
                regionId++;
                PaintRectRegion(w - bw, w, 0, h, regionId | RC_BORDER_REG, chf, srcReg);
                regionId++;
                PaintRectRegion(0, w, 0, bh, regionId | RC_BORDER_REG, chf, srcReg);
                regionId++;
                PaintRectRegion(0, w, h - bh, h, regionId | RC_BORDER_REG, chf, srcReg);
                regionId++;
            }

            chf.borderSize = borderSize;

            int sId = -1;
            while (level > 0)
            {
                level = level >= 2 ? level - 2 : 0;
                sId = (sId + 1) & (NB_STACKS - 1);

                // ctx->StartTimer(RC_TIMER_DIVIDE_TO_LEVELS);

                if (sId == 0)
                {
                    SortCellsByLevel(level, chf, srcReg, NB_STACKS, lvlStacks, 1);
                }
                else
                {
                    AppendStacks(lvlStacks[sId - 1], lvlStacks[sId], srcReg); // copy left overs from last level
                }

                // ctx->StopTimer(RC_TIMER_DIVIDE_TO_LEVELS);

                ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_EXPAND);

                // Expand current regions until no empty connected cells found.
                // 扩展当前区域，直到找不到空的连接单元格。
                ExpandRegions(expandIters, level, chf, srcReg, srcDist, lvlStacks[sId], false);

                ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_EXPAND);

                ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FLOOD);

                // Mark new regions with IDs.
                // 用 ID 标记新区域。
                for (int j = 0; j < lvlStacks[sId].Count; j += 3)
                {
                    int x = lvlStacks[sId][j];
                    int y = lvlStacks[sId][j + 1];
                    int i = lvlStacks[sId][j + 2];
                    if (i >= 0 && srcReg[i] == 0)
                    {
                        if (FloodRegion(x, y, i, level, regionId, chf, srcReg, srcDist, stack))
                        {
                            regionId++;
                        }
                    }
                }

                ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FLOOD);
            }

            // Expand current regions until no empty connected cells found.
            // 扩展当前区域，直到找不到空的连接单元格。
            ExpandRegions(expandIters * 8, 0, chf, srcReg, srcDist, stack, true);

            ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_WATERSHED);

            ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FILTER);

            // Merge regions and filter out small regions.
            // 合并区域并过滤掉小区域。
            List<int> overlaps = new List<int>();
            chf.maxRegions = MergeAndFilterRegions(ctx, minRegionArea, mergeRegionArea, regionId, chf, srcReg, overlaps);

            // If overlapping regions were found during merging, split those regions.
            // 如果在合并过程中发现重叠区域，则分割这些区域。
            if (overlaps.Count > 0)
            {
                ctx.Warn("rcBuildRegions: " + overlaps.Count + " overlapping regions.");
            }

            ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FILTER);

            // Write the result out.
            // 将结果写出来。
            for (int i = 0; i < chf.spanCount; ++i)
            {
                chf.spans[i].reg = srcReg[i];
            }
        }

        // 此方法用于构建分层区域。分层区域是一种将区域划分为多个层次的方法，以便更好地处理复杂的导航网格。这个方法首先标记边界区域，
        // 然后逐行扫描紧凑高度场中的跨度，将它们分配给一个区域。接下来，它合并单调区域以创建非重叠区域，并删除小区域。
        public static void BuildLayerRegions(RcTelemetry ctx, RcCompactHeightfield chf, int minRegionArea)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS);

            int w = chf.width;
            int h = chf.height;
            int borderSize = chf.borderSize;
            int id = 1;

            int[] srcReg = new int[chf.spanCount];
            int nsweeps = Math.Max(chf.width, chf.height);
            RcSweepSpan[] sweeps = new RcSweepSpan[nsweeps];
            for (int i = 0; i < sweeps.Length; i++)
            {
                sweeps[i] = new RcSweepSpan();
            }

            // Mark border regions.
            // 标记边界区域。
            if (borderSize > 0)
            {
                // Make sure border will not overflow.
                // 确保边框不会溢出。
                int bw = Math.Min(w, borderSize);
                int bh = Math.Min(h, borderSize);
                // Paint regions     // 绘制区域
                PaintRectRegion(0, bw, 0, h, id | RC_BORDER_REG, chf, srcReg);
                id++;
                PaintRectRegion(w - bw, w, 0, h, id | RC_BORDER_REG, chf, srcReg);
                id++;
                PaintRectRegion(0, w, 0, bh, id | RC_BORDER_REG, chf, srcReg);
                id++;
                PaintRectRegion(0, w, h - bh, h, id | RC_BORDER_REG, chf, srcReg);
                id++;
            }

            int[] prev = new int[1024];

            // Sweep one line at a time.    // 一次扫描一行。
            for (int y = borderSize; y < h - borderSize; ++y)
            {
                // Collect spans from this row.     // 收集该行的跨度。
                if (prev.Length <= id * 2)
                {
                    prev = new int[id * 2];
                }
                else
                {
                    Array.Fill(prev, 0, 0, (id) - (0));
                }

                int rid = 1;

                for (int x = borderSize; x < w - borderSize; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];

                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        RcCompactSpan s = chf.spans[i];
                        if (chf.areas[i] == RC_NULL_AREA)
                        {
                            continue;
                        }

                        // -x
                        int previd = 0;
                        if (GetCon(s, 0) != RC_NOT_CONNECTED)
                        {
                            int ax = x + GetDirOffsetX(0);
                            int ay = y + GetDirOffsetY(0);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 0);
                            if ((srcReg[ai] & RC_BORDER_REG) == 0 && chf.areas[i] == chf.areas[ai])
                            {
                                previd = srcReg[ai];
                            }
                        }

                        if (previd == 0)
                        {
                            previd = rid++;
                            sweeps[previd].rid = previd;
                            sweeps[previd].ns = 0;
                            sweeps[previd].nei = 0;
                        }

                        // -y
                        if (GetCon(s, 3) != RC_NOT_CONNECTED)
                        {
                            int ax = x + GetDirOffsetX(3);
                            int ay = y + GetDirOffsetY(3);
                            int ai = chf.cells[ax + ay * w].index + GetCon(s, 3);
                            if (srcReg[ai] != 0 && (srcReg[ai] & RC_BORDER_REG) == 0 && chf.areas[i] == chf.areas[ai])
                            {
                                int nr = srcReg[ai];
                                if (sweeps[previd].nei == 0 || sweeps[previd].nei == nr)
                                {
                                    sweeps[previd].nei = nr;
                                    sweeps[previd].ns++;
                                    if (prev.Length <= nr)
                                    {
                                        Array.Resize(ref prev, prev.Length * 2);
                                    }

                                    prev[nr]++;
                                }
                                else
                                {
                                    sweeps[previd].nei = RC_NULL_NEI;
                                }
                            }
                        }

                        srcReg[i] = previd;
                    }
                }

                // Create unique ID.   // 创建唯一 ID。
                for (int i = 1; i < rid; ++i)
                {
                    if (sweeps[i].nei != RC_NULL_NEI && sweeps[i].nei != 0 && prev[sweeps[i].nei] == sweeps[i].ns)
                    {
                        sweeps[i].id = sweeps[i].nei;
                    }
                    else
                    {
                        sweeps[i].id = id++;
                    }
                }

                // Remap IDs    // 重新映射 ID
                for (int x = borderSize; x < w - borderSize; ++x)
                {
                    RcCompactCell c = chf.cells[x + y * w];

                    for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                    {
                        if (srcReg[i] > 0 && srcReg[i] < rid)
                        {
                            srcReg[i] = sweeps[srcReg[i]].id;
                        }
                    }
                }
            }

            ctx.StartTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FILTER);

            // Merge monotone regions to layers and remove small regions.        // 将单调区域合并到图层并删除小区域。
            List<int> overlaps = new List<int>();
            chf.maxRegions = MergeAndFilterLayerRegions(ctx, minRegionArea, id, chf, srcReg, overlaps);

            ctx.StopTimer(RcTimerLabel.RC_TIMER_BUILD_REGIONS_FILTER);

            // Store the result out.      // 将结果存储出来。
            for (int i = 0; i < chf.spanCount; ++i)
            {
                chf.spans[i].reg = srcReg[i];
            }
        }
    }
}