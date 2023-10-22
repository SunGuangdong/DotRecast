using System;
using DotRecast.Core;
using DotRecast.Recast.Demo.Draw;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Tools;
using ImGuiNET;
using Serilog;
using static DotRecast.Recast.Demo.Draw.DebugDraw;

namespace DotRecast.Recast.Demo.Tools;

/// <summary>
/// 这个类用于在3D环境中处理导航网格的瓦片。
/// </summary>
public class TileSampleTool : ISampleTool
{
    private static readonly ILogger Logger = Log.ForContext<TileSampleTool>();

    private DemoSample _sample;
    // 存储RcTileTool对象，用于处理导航网格的瓦片。
    private readonly RcTileTool _tool;

    // 用于存储鼠标点击的位置信息。
    private bool _hitPosSet;
    private RcVec3f _hitPos;

    public TileSampleTool()
    {
        _tool = new();
    }
    // 存储RcTileTool对象，用于处理导航网格的瓦片。
    public void Layout()
    {
        var geom = _sample.GetInputGeom();
        var settings = _sample.GetSettings();
        var navMesh = _sample.GetNavMesh();

        if (ImGui.Button("Create All Tile"))
        {
            _tool.BuildAllTiles(geom, settings, navMesh);
        }

        if (ImGui.Button("Remove All Tile"))
        {
            _tool.RemoveAllTiles(geom, settings, navMesh);
        }
    }
    // 此方法用于在场景中渲染导航网格和瓦片。它使用RecastDebugDraw类来执行实际的渲染操作。
    public void HandleRender(NavMeshRenderer renderer)
    {
        var geom = _sample.GetInputGeom();
        var settings = _sample.GetSettings();

        if (null == geom)
            return;

        var dd = renderer.GetDebugDraw();
        if (_hitPosSet)
        {
            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();

            var s = settings.agentRadius;

            float ts = settings.tileSize * settings.cellSize;
            int tx = (int)((_hitPos.x - bmin[0]) / ts);
            int ty = (int)((_hitPos.z - bmin[2]) / ts);

            RcVec3f lastBuiltTileBmin = RcVec3f.Zero;
            RcVec3f lastBuiltTileBmax = RcVec3f.Zero;

            lastBuiltTileBmin[0] = bmin[0] + tx * ts;
            lastBuiltTileBmin[1] = bmin[1];
            lastBuiltTileBmin[2] = bmin[2] + ty * ts;

            lastBuiltTileBmax[0] = bmin[0] + (tx + 1) * ts;
            lastBuiltTileBmax[1] = bmax[1];
            lastBuiltTileBmax[2] = bmin[2] + (ty + 1) * ts;

            dd.DebugDrawCross(_hitPos.x, _hitPos.y + 0.1f, _hitPos.z, s, DuRGBA(0, 0, 0, 128), 2.0f);
            dd.DebugDrawBoxWire(
                lastBuiltTileBmin.x, lastBuiltTileBmin.y, lastBuiltTileBmin.z,
                lastBuiltTileBmax.x, lastBuiltTileBmax.y, lastBuiltTileBmax.z,
                DuRGBA(255, 255, 255, 64), 1.0f);

            // 표기
        }
    }

    public IRcToolable GetTool()
    {
        return _tool;
    }

    public void SetSample(DemoSample sample)
    {
        _sample = sample;
    }

    public void OnSampleChanged()
    {
    }

    // 此方法用于处理鼠标点击事件。根据是否按下Shift键，它会执行不同的操作，如创建或删除瓦片。
    public void HandleClick(RcVec3f s, RcVec3f p, bool shift)
    {
        _hitPosSet = true;
        _hitPos = p;

        var geom = _sample.GetInputGeom();
        var settings = _sample.GetSettings();
        var navMesh = _sample.GetNavMesh();

        if (shift)
        {
            _tool.RemoveTile(geom, settings, navMesh, _hitPos);
        }
        else
        {
            bool built = _tool.BuildTile(geom, settings, navMesh, _hitPos, out var tileBuildTicks, out var tileTriCount, out var tileMemUsage);
            if (!built)
            {
                Logger.Error($"failed to build tile - check!");
            }
            else
            {
                Logger.Information($"{tileBuildTicks / (float)TimeSpan.TicksPerMillisecond}ms / {tileTriCount}Tris / {tileMemUsage}kB ");
            }
        }
    }


    public void HandleUpdate(float dt)
    {
    }

    public void HandleClickRay(RcVec3f start, RcVec3f direction, bool shift)
    {
    }
}