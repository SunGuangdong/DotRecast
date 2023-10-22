using System;
using System.IO;
using System.Runtime.InteropServices;
using DotRecast.Core;

namespace DotRecast.Recast.Demo.Draw;

/// <summary>
/// 它表示一个OpenGL顶点，包含了顶点的位置、纹理坐标和颜色信息。
/// 这个结构体提供了一种通用的表示顶点数据的方法，可以用于实现各种OpenGL绘制任务。
/// 在使用这个结构体时，需要根据具体的绘制需求创建相应的顶点数据。
///       这个结构体使用了显式布局（LayoutKind.Explicit），可以精确控制每个成员变量在内存中的偏移量，以便与OpenGL的顶点属性指针对应。
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct OpenGLVertex
{
    // 分别表示顶点的x、y、z坐标。
    [FieldOffset(0)]
    private readonly float x;

    [FieldOffset(4)]
    private readonly float y;

    [FieldOffset(8)]
    private readonly float z;

    // 分别表示顶点的纹理坐标u、v。
    [FieldOffset(12)]
    private readonly float u;

    [FieldOffset(16)]
    private readonly float v;

    // 表示顶点的颜色。
    [FieldOffset(20)]
    private readonly int color;

    // 这个结构体提供了多个构造函数，用于创建不同类型的顶点。可以接收不同类型的顶点位置（如float数组、RcVec3f对象等）和颜色信息，以及可选的纹理坐标。
    public OpenGLVertex(RcVec3f pos, RcVec2f uv, int color) :
        this(pos.x, pos.y, pos.z, uv.x, uv.y, color)
    {
    }

    public OpenGLVertex(float[] pos, int color) :
        this(pos[0], pos[1], pos[2], 0f, 0f, color)
    {
    }

    public OpenGLVertex(RcVec3f pos, int color) :
        this(pos.x, pos.y, pos.z, 0f, 0f, color)
    {
    }


    public OpenGLVertex(float x, float y, float z, int color) :
        this(x, y, z, 0f, 0f, color)
    {
    }

    public OpenGLVertex(float x, float y, float z, float u, float v, int color)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.color = color;
    }

    // 将顶点数据写入BinaryWriter对象。这个方法将顶点的位置、纹理坐标和颜色信息写入给定的BinaryWriter对象，用于序列化顶点数据。
    public void Store(BinaryWriter writer)
    {
        // writer.Write(BitConverter.GetBytes(x));
        // writer.Write(BitConverter.GetBytes(y));
        // writer.Write(BitConverter.GetBytes(z));
        // writer.Write(BitConverter.GetBytes(u));
        // writer.Write(BitConverter.GetBytes(v));
        // writer.Write(BitConverter.GetBytes(color));

        writer.Write(x);
        writer.Write(y);
        writer.Write(z);
        writer.Write(u);
        writer.Write(v);
        writer.Write(color);
    }
}