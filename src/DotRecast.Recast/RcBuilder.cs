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
using System.Threading;
using System.Threading.Tasks;
using DotRecast.Core;
using DotRecast.Recast.Geom;

namespace DotRecast.Recast
{
    using static RcCommons;
    using static RcAreas;

    /// <summary>
    /// 公共类，用于构建导航网格。它包含一些方法，用于在输入几何体上生成导航网格的不同部分。
    /// 此类的主要目的是在输入几何体上生成导航网格，可以在游戏或仿真环境中用于路径规划和导航。
    /// </summary>
    public class RcBuilder
    {
        private readonly IRcBuilderProgressListener _progressListener;

        public RcBuilder()
        {
            _progressListener = null;
        }

        public RcBuilder(IRcBuilderProgressListener progressListener)
        {
            _progressListener = progressListener;
        }

        /// <summary>
        /// 此方法接受一个输入几何提供者、配置对象和任务工厂，并返回一个RcBuilderResult列表。
        /// 它会计算网格的边界，然后根据提供的任务工厂是单线程还是多线程方式构建导航网格。
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="cfg"></param>
        /// <param name="taskFactory"></param>
        /// <returns></returns>
        public List<RcBuilderResult> BuildTiles(IInputGeomProvider geom, RcConfig cfg, TaskFactory taskFactory)
        {
            // 获取输入几何体的边界最小值（bmin）和最大值（bmax）。
            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();
            // 根据边界值、网格大小和配置中的单元大小计算网格的瓦片数量（tw和th）。
            CalcTileCount(bmin, bmax, cfg.Cs, cfg.TileSizeX, cfg.TileSizeZ, out var tw, out var th);
            // 用于存储构建的瓦片结果。
            List<RcBuilderResult> results = new List<RcBuilderResult>();
            if (null != taskFactory)
            {
                BuildMultiThreadAsync(geom, cfg, bmin, bmax, tw, th, results, taskFactory, default);
            }
            else
            {
                BuildSingleThreadAsync(geom, cfg, bmin, bmax, tw, th, results);
            }

            return results;
        }


        /// <summary>
        /// 此方法类似于BuildTiles()，但以异步方式执行。它返回一个表示任务完成情况的Task对象。
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="cfg"></param>
        /// <param name="threads"></param>
        /// <param name="results"></param>
        /// <param name="taskFactory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task BuildTilesAsync(IInputGeomProvider geom, RcConfig cfg, int threads, List<RcBuilderResult> results, TaskFactory taskFactory, CancellationToken cancellationToken)
        {
            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();
            CalcTileCount(bmin, bmax, cfg.Cs, cfg.TileSizeX, cfg.TileSizeZ, out var tw, out var th);
            Task task;
            if (1 < threads)
            {
                task = BuildMultiThreadAsync(geom, cfg, bmin, bmax, tw, th, results, taskFactory, cancellationToken);
            }
            else
            {
                task = BuildSingleThreadAsync(geom, cfg, bmin, bmax, tw, th, results);
            }

            return task;
        }

        /// <summary>
        /// BuildSingleThreadAsync() 和 BuildMultiThreadAsync()：这两个方法用于根据输入几何和配置在单线程或多线程模式下构建导航网格。
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="cfg"></param>
        /// <param name="bmin"></param>
        /// <param name="bmax"></param>
        /// <param name="tw">网格的瓦片数量（宽度 ）</param>
        /// <param name="th">高度</param>
        /// <param name="results"></param>
        /// <returns></returns>
        private Task BuildSingleThreadAsync(IInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax,
            int tw, int th, List<RcBuilderResult> results)
        {
            // 用于跟踪构建的瓦片数量
            RcAtomicInteger counter = new RcAtomicInteger(0);
            // 外层循环遍历高度（th）
            for (int y = 0; y < th; ++y)
            {
                // 内层循环遍历宽度（tw）
                for (int x = 0; x < tw; ++x)
                {
                    // 对于每个瓦片，调用BuildTile()方法，使用输入几何体、配置、边界值、当前瓦片的坐标（x和y）以及counter和总瓦片数量（tw * th）构建一个导航网格瓦片。
                    results.Add(BuildTile(geom, cfg, bmin, bmax, x, y, counter, tw * th));
                }
            }

            return Task.CompletedTask;
        }

        // 用于在多线程模式下构建导航网格瓦片。
        /*
         * geom：输入几何体提供者（IInputGeomProvider）。
            cfg：导航网格的配置（RcConfig）。
            bmin和bmax：输入几何体的边界最小值和最大值（RcVec3f）。
            tw和th：网格的瓦片数量（宽度和高度）。
            results：一个RcBuilderResult类型的列表，用于存储构建的瓦片结果。
            taskFactory：用于创建和启动任务的TaskFactory对象。
            cancellationToken：用于在任务执行过程中取消操作的CancellationToken。
         */
        private Task BuildMultiThreadAsync(IInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax,
            int tw, int th, List<RcBuilderResult> results, TaskFactory taskFactory, CancellationToken cancellationToken)
        {
            // 用于跟踪构建的瓦片数量。
            RcAtomicInteger counter = new RcAtomicInteger(0);
            // 用于同步多个任务。初始计数设置为瓦片总数（tw * th）。
            CountdownEvent latch = new CountdownEvent(tw * th);
            // 用于存储每个瓦片的构建任务。
            List<Task> tasks = new List<Task>();

            // 循环遍历网格的每个瓦片。外层循环遍历宽度（tw），内层循环遍历高度（th）。
            for (int x = 0; x < tw; ++x)
            {
                for (int y = 0; y < th; ++y)
                {
                    int tx = x;
                    int ty = y;
                    // 对于每个瓦片，创建一个新的任务，该任务调用BuildTile()方法，并使用输入几何体、配置、边界值、当前瓦片的坐标（tx和ty）以及counter和总瓦片数量（tw * th）构建一个导航网格瓦片。
                    var task = taskFactory.StartNew(() =>
                    {
                        // 对于每个瓦片，创建一个新的任务，该任务调用BuildTile()方法，并使用输入几何体、配置、边界值、当前瓦片的坐标（tx和ty）以及counter和总瓦片数量（tw * th）构建一个导航网格瓦片。
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        // 尝试执行BuildTile()方法并捕获任何异常。在成功构建瓦片后，将其添加到results列表中。注意，由于多线程环境，需要使用lock语句确保对results列表的访问是线程安全的。
                        try
                        {
                            RcBuilderResult tile = BuildTile(geom, cfg, bmin, bmax, tx, ty, counter, tw * th);
                            lock (results)
                            {
                                results.Add(tile);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        
                        // 在任务完成后，调用latch.Signal()方法递减latch的计数。
                        latch.Signal();
                    }, cancellationToken);

                    tasks.Add(task);
                }
            }

            // 在所有任务创建后，调用latch.Wait()方法等待所有任务完成。如果线程在等待时被中断，将捕获ThreadInterruptedException异常。
            try
            {
                latch.Wait();
            }
            catch (ThreadInterruptedException)
            {
            }

            // 该方法等待tasks列表中的所有任务完成。
            return Task.WhenAll(tasks.ToArray());
        }

        /// <summary>
        /// 此方法根据给定的输入几何、配置和网格边界构建一个导航网格瓦片，并返回一个RcBuilderResult对象。
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="cfg"></param>
        /// <param name="bmin"></param>
        /// <param name="bmax"></param>
        /// <param name="tx"></param>
        /// <param name="ty"></param>
        /// <param name="counter"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public RcBuilderResult BuildTile(IInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax, int tx,
            int ty, RcAtomicInteger counter, int total)
        {
            RcBuilderResult result = Build(geom, new RcBuilderConfig(cfg, bmin, bmax, tx, ty));
            if (_progressListener != null)
            {
                _progressListener.OnProgress(counter.IncrementAndGet(), total);
            }

            return result;
        }

        /// <summary>
        /// ！！！！！ 重要入口 ！！！！！
        /// 
        /// 此方法接受一个输入几何提供者和RcBuilderConfig对象，然后构建一个导航网格。
        /// 它首先将输入几何体栅格化为高度场，然后过滤高度场以生成一个紧凑高度场，并根据配置对其进行分区。
        /// 最后，它从轮廓创建多边形网格，并为每个多边形创建细节网格。
        /// </summary>
        /// <param name="geom"></param> 
        /// <param name="builderCfg"></param>
        /// <returns></returns>
        public RcBuilderResult Build(IInputGeomProvider geom, RcBuilderConfig builderCfg)
        {
            RcConfig cfg = builderCfg.cfg;
            // 用于收集构建过程中的性能数据和日志信息。
            RcTelemetry ctx = new RcTelemetry();
            //
            // Step 1. Rasterize input polygon soup.
            // 步骤 1. 栅格化输入多边形汤。
            
            // 根据输入几何体和builderCfg对象生成一个实心高度场（RcHeightfield）。
            RcHeightfield solid = RcVoxelizations.BuildSolidHeightfield(geom, builderCfg, ctx);
            return Build(builderCfg.tileX, builderCfg.tileZ, geom, cfg, solid, ctx);
        }

        // 构建导航网格
        /*
         *  tileX和tileZ：瓦片在网格中的坐标（X和Z轴）。
            geom：输入几何体提供者（IInputGeomProvider）。
            cfg：导航网格的配置（RcConfig）。
            solid：已经栅格化的实心高度场（RcHeightfield）。
            ctx：用于收集构建过程中的性能数据和日志信息的RcTelemetry对象。
         */
        public RcBuilderResult Build(int tileX, int tileZ, IInputGeomProvider geom, RcConfig cfg, RcHeightfield solid, RcTelemetry ctx)
        {
            // 使用配置和上下文过滤实心高度场。
            FilterHeightfield(solid, cfg, ctx);
            // 使用输入几何体、配置、上下文和实心高度场创建一个紧凑高度场（RcCompactHeightfield）。
            RcCompactHeightfield chf = BuildCompactHeightfield(geom, cfg, ctx, solid);

            // Partition the heightfield so that we can use simple algorithm later to triangulate the walkable areas.
            // There are 3 partitioning methods, each with some pros and cons:
            // 1) Watershed partitioning
            // - the classic Recast partitioning
            // - creates the nicest tessellation
            // - usually slowest
            // - partitions the heightfield into nice regions without holes or  overlaps
            // - the are some corner cases where this method creates produces holes and overlaps
            // - holes may appear when a small obstacles is close to large open area  (triangulation can handle this)
            // - overlaps may occur if you have narrow spiral corridors (i.e  stairs), this make triangulation to fail
            // * generally the best choice if you precompute the navmesh, use this if you have large open areas
            // 2) Monotone partioning
            // - fastest
            // - partitions the heightfield into regions without holes and overlaps  (guaranteed)
            // - creates long thin polygons, which sometimes causes paths with  detours
            // * use this if you want fast navmesh generation
            // 3) Layer partitoining
            // - quite fast
            // - partitions the heighfield into non-overlapping regions
            // - relies on the triangulation code to cope with holes (thus slower  than monotone partitioning)
            // - produces better triangles than monotone partitioning
            // - does not have the corner cases of watershed partitioning
            // - can be slow and create a bit ugly tessellation (still better than  monotone)  if you have large open areas with small obstacles (not a problem if  you use tiles)
            // * good choice to use for tiled navmesh with medium and small sized tiles
            
            
            // 对高度场进行分区，以便我们稍后可以使用简单的算法对可步行区域进行三角测量。
            // 有 3 种分区方法，每种方法都有一些优点和缺点：
            // 1) 流域划分
                // - 经典的 Recast 分区
                // - 创建最好的镶嵌
                // - 通常最慢
                // - 将高度场划分为没有孔洞或重叠的漂亮区域
                // - 该方法在一些极端情况下会产生孔洞和重叠
                // - 当小障碍物靠近大的开放区域时可能会出现洞（三角测量可以处理这个问题）
                // - 如果您有狭窄的螺旋走廊（即楼梯），可能会发生重叠，这会使三角测量失败
                // * 如果您预先计算导航网格，通常是最佳选择，如果您有较大的开放区域，请使用此选项
            // 2) 单调分区
                // - 最快的
                // - 将高度场划分为没有孔和重叠的区域（保证）
                // - 创建细长的多边形，这有时会导致路径绕道
                // * 如果您想要快速生成导航网格，请使用此选项
            // 3) 层划分
                // - 蛮快
                // - 将高度场划分为不重叠的区域
                // - 依赖三角剖分代码来处理漏洞（因此比单调分区慢）
                // - 产生比单调分区更好的三角形
                // - 没有分水岭分区的极端情况
                // - 如果你有大的开放区域和小障碍物（如果你使用瓷砖，这不是问题），那么速度可能会很慢，并且会产生一些丑陋的镶嵌（仍然比单调更好）
                // * 用于具有中型和小型瓷砖的平铺导航网格的不错选择
            
            

            // 根据配置中的分区类型（cfg.Partition），选择合适的分区方法。可以选择分区类型有：
            //    水域分区（Watershed partitioning）
            //    单调分区（Monotone partitioning）
            //    层分区（Layer partitioning）
            // 对于每种分区类型，调用相应的方法. 来构建导航网格的区域。
            if (cfg.Partition == RcPartitionType.WATERSHED.Value)
            {
                // Prepare for region partitioning, by calculating distance field along the walkable surface.
                // 通过计算沿walkable表面的距离场来准备区域划分。
                RcRegions.BuildDistanceField(ctx, chf);
                // Partition the walkable surface into simple regions without holes.
                // 将可步行表面划分为没有洞的简单区域。
                RcRegions.BuildRegions(ctx, chf, cfg.MinRegionArea, cfg.MergeRegionArea);
            }
            else if (cfg.Partition == RcPartitionType.MONOTONE.Value)
            {
                // Partition the walkable surface into simple regions without holes.
                // Monotone partitioning does not need distancefield.
                // 将walkable表面划分为没有洞的简单区域。
                // 单调划分不需要距离场。
                RcRegions.BuildRegionsMonotone(ctx, chf, cfg.MinRegionArea, cfg.MergeRegionArea);
            }
            else
            {
                // Partition the walkable surface into simple regions without holes.
                // 将walkable表面划分为没有孔洞的简单区域。
                RcRegions.BuildLayerRegions(ctx, chf, cfg.MinRegionArea);
            }

            //
            // Step 5. Trace and simplify region contours.
            // Step 5. 使用上下文、紧凑高度场、配置中的最大简化误差、最大边长和轮廓构建标志来创建轮廓集（RcContourSet）。

            // Create contours.
            RcContourSet cset = RcContours.BuildContours(ctx, chf, cfg.MaxSimplificationError, cfg.MaxEdgeLen, RcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES);

            //
            // Step 6. Build polygons mesh from contours.
            // Step 6. 使用上下文、轮廓集和配置中的每个多边形的最大顶点数来构建多边形网格（RcPolyMesh）。

            RcPolyMesh pmesh = RcMeshs.BuildPolyMesh(ctx, cset, cfg.MaxVertsPerPoly);

            //
            // Step 7. Create detail mesh which allows to access approximate height on each polygon.
            // 如果配置中的BuildMeshDetail为true，则调用RcMeshDetails.BuildPolyMeshDetail()方法，
            // 使用上下文、多边形网格、紧凑高度场以及配置中的详细采样距离和最大误差来创建详细的多边形网格（RcPolyMeshDetail）。否则，将dmesh设置为null。
            RcPolyMeshDetail dmesh = cfg.BuildMeshDetail
                ? RcMeshDetails.BuildPolyMeshDetail(ctx, pmesh, chf, cfg.DetailSampleDist, cfg.DetailSampleMaxError)
                : null;
            
            // 返回一个新的RcBuilderResult对象，其中包含瓦片坐标、实心高度场、紧凑高度场、轮廓集、多边形网格、详细多边形网格和上下文。
            return new RcBuilderResult(tileX, tileZ, solid, chf, cset, pmesh, dmesh, ctx);
        }

        /*
         * Step 2. Filter walkable surfaces.
         * 此方法根据配置过滤高度场，以消除不需要的悬垂和不可站立的跨度。
         *
         *  solid：实心高度场（RcHeightfield）。
            cfg：导航网格的配置（RcConfig）。
            ctx：用于收集构建过程中的性能数据和日志信息的RcTelemetry对象。
         */
        private void FilterHeightfield(RcHeightfield solid, RcConfig cfg, RcTelemetry ctx)
        {
            // Once all geometry is rasterized, we do initial pass of filtering to
            // remove unwanted overhangs caused by the conservative rasterization
            // as well as filter spans where the character cannot possibly stand.
            // 一旦所有几何图形都被光栅化，我们就会进行初始过滤，以消除保守光栅化引起的不需要的悬垂以及角色无法站立的过滤范围。
            
            // 如果配置中的FilterLowHangingObstacles为true， 使用上下文、可行走攀爬高度和实心高度场来过滤低悬挂的可行走障碍物。
            if (cfg.FilterLowHangingObstacles)
            {
                RcFilters.FilterLowHangingWalkableObstacles(ctx, cfg.WalkableClimb, solid);
            }

            // 如果配置中的FilterLedgeSpans为true， 使用上下文、可行走高度、可行走攀爬高度和实心高度场来过滤悬崖跨度。
            if (cfg.FilterLedgeSpans)
            {
                RcFilters.FilterLedgeSpans(ctx, cfg.WalkableHeight, cfg.WalkableClimb, solid);
            }

            // 如果配置中的FilterWalkableLowHeightSpans为true， 使用上下文、可行走高度和实心高度场来过滤可行走的低高度跨度。
            if (cfg.FilterWalkableLowHeightSpans)
            {
                RcFilters.FilterWalkableLowHeightSpans(ctx, cfg.WalkableHeight, solid);
            }
        }

        /*
         * Step 3. Partition walkable surface to simple regions.
         * 此方法将高度场压缩为紧凑高度场，以便更快地处理。然后对可行走区域进行侵蚀，并根据输入几何体标记区域。
         *  geom：输入几何体提供者（IInputGeomProvider）。
            cfg：导航网格的配置（RcConfig）。
            ctx：用于收集构建过程中的性能数据和日志信息的RcTelemetry对象。
            solid：实心高度场（RcHeightfield）。
         */
        private RcCompactHeightfield BuildCompactHeightfield(IInputGeomProvider geom, RcConfig cfg, RcTelemetry ctx, RcHeightfield solid)
        {
            // Compact the heightfield so that it is faster to handle from now on.
            // This will result more cache coherent data as well as the neighbours
            // between walkable cells will be calculated.
            // 压缩高度场，以便从现在开始处理起来更快。 这将导致更多的缓存一致数据以及可步行单元之间的邻居将被计算。
            
            // 使用上下文、可行走高度、可行走攀爬高度和实心高度场来构建紧凑高度场（RcCompactHeightfield）。
            RcCompactHeightfield chf = RcCompacts.BuildCompactHeightfield(ctx, cfg.WalkableHeight, cfg.WalkableClimb, solid);

            // Erode the walkable area by agent radius.     按代理半径侵蚀可步行区域。
            // 使用上下文、可行走半径和紧凑高度场来侵蚀可行走区域。
            ErodeWalkableArea(ctx, cfg.WalkableRadius, chf);
            // (Optional) Mark areas.      （可选）标记区域。   
            // 如果geom不为null，则遍历输入几何体的凸体积集合。
            if (geom != null)
            {
                // 对于每个凸体积，调用MarkConvexPolyArea()方法，使用上下文、顶点集、最小高度、最大高度、区域修改器和紧凑高度场来标记凸多边形区域。
                foreach (RcConvexVolume vol in geom.ConvexVolumes())
                {
                    MarkConvexPolyArea(ctx, vol.verts, vol.hmin, vol.hmax, vol.areaMod, chf);
                }
            }

            return chf;
        }

        /// <summary>
        /// 它接受一个输入几何提供者（IInputGeomProvider）和一个RcBuilderConfig对象。
        /// 这个方法的主要目的是构建一个高度场层集，用于表示导航网格的层次结构。这可以用于进一步处理和生成导航网格。
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="builderCfg"></param>
        /// <returns></returns>
        public RcHeightfieldLayerSet BuildLayers(IInputGeomProvider geom, RcBuilderConfig builderCfg)
        {
            // 用于收集构建过程中的性能数据和日志信息。
            RcTelemetry ctx = new RcTelemetry();
            // 根据输入几何体和builderCfg对象生成一个实心高度场（RcHeightfield）。
            RcHeightfield solid = RcVoxelizations.BuildSolidHeightfield(geom, builderCfg, ctx);
            // 使用builderCfg.cfg配置过滤高度场，以消除不需要的悬垂和不可站立的跨度。
            FilterHeightfield(solid, builderCfg.cfg, ctx);
            // 使用输入几何体、配置、上下文和实心高度场创建一个紧凑高度场（RcCompactHeightfield）。
            RcCompactHeightfield chf = BuildCompactHeightfield(geom, builderCfg.cfg, ctx, solid);
            // 使用上下文、紧凑高度场和可行走高度构建一个高度场层集（RcHeightfieldLayerSet）。
            return RcLayers.BuildHeightfieldLayers(ctx, chf, builderCfg.cfg.WalkableHeight);
        }
    }
}