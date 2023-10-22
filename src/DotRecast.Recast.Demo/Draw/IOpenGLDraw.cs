using DotRecast.Core;

namespace DotRecast.Recast.Demo.Draw;

/*
 * 这个IOpenGLDraw接口定义了一组用于OpenGL绘制的方法。实现这个接口的类可以用于处理OpenGL中的绘制任务。以下是这个接口中方法的简要说明：
    Init方法：初始化绘制环境。
    Clear方法：清除绘制内容。
    Begin方法：开始一个绘制任务，需要指定绘制图元类型（DebugDrawPrimitives）和大小（size）。
    End方法：结束一个绘制任务。
    Vertex方法：有多个重载版本，用于绘制顶点。可以接收不同类型的顶点位置（如float数组、RcVec3f对象等）和颜色信息，以及可选的纹理坐标。
    Fog方法：有两个重载版本，用于控制雾效果。一个接收布尔值（state）来开启或关闭雾效果，另一个接收两个浮点数（start, end）来设置雾的开始和结束距离。
    DepthMask方法：控制深度缓冲的写入。接收一个布尔值（state）来开启或关闭深度缓冲的写入。
    Texture方法：用于绑定或解绑一个GLCheckerTexture纹理。接收一个GLCheckerTexture对象（g_tex）和一个布尔值（state）来表示是否绑定纹理。
    ProjectionMatrix方法：设置投影矩阵。接收一个RcMatrix4x4f类型的投影矩阵（projectionMatrix）。
    ViewMatrix方法：设置视图矩阵。接收一个RcMatrix4x4f类型的视图矩阵（viewMatrix）。
 */
public interface IOpenGLDraw
{
    void Init();

    void Clear();

    void Begin(DebugDrawPrimitives prim, float size);

    void End();

    void Vertex(float x, float y, float z, int color);

    void Vertex(float[] pos, int color);
    void Vertex(RcVec3f pos, int color);

    void Vertex(RcVec3f pos, int color, RcVec2f uv);

    void Vertex(float x, float y, float z, int color, float u, float v);

    void Fog(bool state);

    void DepthMask(bool state);

    void Texture(GLCheckerTexture g_tex, bool state);

    void ProjectionMatrix(ref RcMatrix4x4f projectionMatrix);

    void ViewMatrix(ref RcMatrix4x4f viewMatrix);

    void Fog(float start, float end);
}