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
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Demo.Draw;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Tools;
using ImGuiNET;
using Serilog;
using static DotRecast.Recast.Demo.Draw.DebugDraw;
using static DotRecast.Recast.Demo.Draw.DebugDrawPrimitives;

namespace DotRecast.Recast.Demo.Tools;

/// <summary>
/// 实现了ISampleTool接口，它是一个用于处理凸体样本的工具。该类包含了一些用于处理和渲染凸体形状的方法。以下是类中主要成员的简要说明：
/// 
/// 该类的主要目的是处理和渲染凸体形状。它使用RcConvexVolumeTool类来执行凸体相关操作，如添加、删除和创建凸体。
/// 类中的方法允许用户通过点击和拖动来创建和调整凸体形状，并通过ImGui库提供的UI控件调整参数。当凸体形状发生变化时，HandleRender方法负责渲染它们。
/// </summary>
public class ConvexVolumeSampleTool : ISampleTool
{
    // 一个静态的ILogger实例，用于记录日志。
    private static readonly ILogger Logger = Log.ForContext<ConvexVolumeSampleTool>();
    // 一个DemoSample实例，表示样本数据。
    private DemoSample _sample;
    // 一个RcConvexVolumeTool实例，用于处理凸体相关操作。
    private readonly RcConvexVolumeTool _tool;
    // 浮点数，表示凸体形状的高度、下降和多边形偏移。
    private float _boxHeight = 6f;
    private float _boxDescent = 1f;
    private float _polyOffset = 0f;
    // 表示凸体区域类型的整数值和RcAreaModification实例。
    private int _areaTypeValue = SampleAreaModifications.SAMPLE_AREAMOD_GRASS.Value;
    private RcAreaModification _areaType = SampleAreaModifications.SAMPLE_AREAMOD_GRASS;

    // 初始化一个新的RcConvexVolumeTool实例。
    public ConvexVolumeSampleTool()
    {
        _tool = new RcConvexVolumeTool();
    }
    
    // 使用ImGui库呈现和调整工具参数。
    public void Layout()
    {
        ImGui.SliderFloat("Shape Height", ref _boxHeight, 0.1f, 20f, "%.1f");
        ImGui.SliderFloat("Shape Descent", ref _boxDescent, 0.1f, 20f, "%.1f");
        ImGui.SliderFloat("Poly Offset", ref _polyOffset, 0.1f, 10f, "%.1f");
        ImGui.NewLine();

        int prevAreaTypeValue = _areaTypeValue;

        ImGui.Text("Area Type");
        ImGui.Separator();
        ImGui.RadioButton("Ground", ref _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_GROUND.Value);
        ImGui.RadioButton("Water", ref _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_WATER.Value);
        ImGui.RadioButton("Road", ref _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_ROAD.Value);
        ImGui.RadioButton("Door", ref _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_DOOR.Value);
        ImGui.RadioButton("Grass", ref _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_GRASS.Value);
        ImGui.RadioButton("Jump", ref _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_JUMP.Value);
        ImGui.NewLine();

        if (prevAreaTypeValue != _areaTypeValue)
        {
            _areaType = SampleAreaModifications.OfValue(_areaTypeValue);
        }

        if (ImGui.Button("Clear Shape"))
        {
            _tool.ClearShape();
        }

        if (ImGui.Button("Remove All"))
        {
            _tool.ClearShape();

            var geom = _sample.GetInputGeom();
            if (geom != null)
            {
                geom.ClearConvexVolumes();
            }
        }
    }
    
    // 渲染凸体形状
    public void HandleRender(NavMeshRenderer renderer)
    {
        RecastDebugDraw dd = renderer.GetDebugDraw();

        var pts = _tool.GetShapePoint();
        var hull = _tool.GetShapeHull();

        // Find height extent of the shape.
        float minh = float.MaxValue, maxh = 0;
        for (int i = 0; i < pts.Count; ++i)
        {
            minh = Math.Min(minh, pts[i].y);
        }

        minh -= _boxDescent;
        maxh = minh + _boxHeight;

        dd.Begin(POINTS, 4.0f);
        for (int i = 0; i < pts.Count; ++i)
        {
            int col = DuRGBA(255, 255, 255, 255);
            if (i == pts.Count - 1)
            {
                col = DuRGBA(240, 32, 16, 255);
            }

            dd.Vertex(pts[i].x, pts[i].y + 0.1f, pts[i].z, col);
        }

        dd.End();

        dd.Begin(LINES, 2.0f);
        for (int i = 0, j = hull.Count - 1; i < hull.Count; j = i++)
        {
            int vi = hull[j];
            int vj = hull[i];
            dd.Vertex(pts[vj].x, minh, pts[vj].z, DuRGBA(255, 255, 255, 64));
            dd.Vertex(pts[vi].x, minh, pts[vi].z, DuRGBA(255, 255, 255, 64));
            dd.Vertex(pts[vj].x, maxh, pts[vj].z, DuRGBA(255, 255, 255, 64));
            dd.Vertex(pts[vi].x, maxh, pts[vi].z, DuRGBA(255, 255, 255, 64));
            dd.Vertex(pts[vj].x, minh, pts[vj].z, DuRGBA(255, 255, 255, 64));
            dd.Vertex(pts[vj].x, maxh, pts[vj].z, DuRGBA(255, 255, 255, 64));
        }

        dd.End();
    }

    public IRcToolable GetTool()
    {
        return _tool;
    }
    // 设置_sample实例。
    public void SetSample(DemoSample sample)
    {
        _sample = sample;
    }

    // 处理样本更改事件。
    public void OnSampleChanged()
    {
        // ..
    }
    // 处理点击事件，添加或删除凸体。
    public void HandleClick(RcVec3f s, RcVec3f p, bool shift)
    {
        var geom = _sample.GetInputGeom();
        if (shift)
        {
            _tool.RemoveByPos(geom, p);
        }
        else
        {
            if (_tool.PlottingShape(p, out var pts, out var hull))
            {
                var vol = RcConvexVolumeTool.CreateConvexVolume(pts, hull, _areaType, _boxDescent, _boxHeight, _polyOffset);
                _tool.Add(geom, vol);
            }
        }
    }

    // 处理更新事件。
    public void HandleUpdate(float dt)
    {
        // TODO Auto-generated method stub
    }
    // 处理射线点击事件。
    public void HandleClickRay(RcVec3f start, RcVec3f direction, bool shift)
    {
    }
}