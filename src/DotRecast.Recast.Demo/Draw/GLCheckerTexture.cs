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

using Silk.NET.OpenGL;

namespace DotRecast.Recast.Demo.Draw;

/// <summary>
/// 用于在OpenGL中创建和绑定一个带有棋盘纹理的2D纹理
/// </summary>
public class GLCheckerTexture
{
    private readonly GL _gl;
    private uint m_texId;

    // 构造函数：接收一个GL对象作为参数，用于执行OpenGL操作
    public GLCheckerTexture(GL gl)
    {
        _gl = gl;
    }

    // 释放纹理资源。如果纹理ID不为0（即已创建纹理），则调用_gl.DeleteTextures方法删除纹理。
    public void Release()
    {
        if (m_texId != 0)
        {
            _gl.DeleteTextures(1, m_texId);
        }
    }

    // 绑定纹理。如果纹理ID为0（即未创建纹理），则创建一个带有棋盘纹理的2D纹理，并设置纹理参数。
    // 如果纹理已经创建，则直接绑定纹理。创建纹理时，使用了多级纹理（Mipmap），这可以提高纹理的渲染效果。
    public void Bind()
    {
        if (m_texId == 0)
        {
            // Create checker pattern.
            int col0 = DebugDraw.DuRGBA(215, 215, 215, 255);
            int col1 = DebugDraw.DuRGBA(255, 255, 255, 255);
            uint TSIZE = 64;
            int[] data = new int[TSIZE * TSIZE];

            _gl.GenTextures(1, out m_texId);
            _gl.BindTexture(GLEnum.Texture2D, m_texId);

            int level = 0;
            uint size = TSIZE;
            while (size > 0)
            {
                for (int y = 0; y < size; ++y)
                {
                    for (int x = 0; x < size; ++x)
                    {
                        data[x + y * size] = (x == 0 || y == 0) ? col0 : col1;
                    }
                }

                _gl.TexImage2D<int>(GLEnum.Texture2D, level, InternalFormat.Rgba, size, size, 0, GLEnum.Rgba, GLEnum.UnsignedByte, data);
                size /= 2;
                level++;
            }

            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (uint)GLEnum.LinearMipmapNearest);
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (uint)GLEnum.Linear);
        }
        else
        {
            _gl.BindTexture(GLEnum.Texture2D, m_texId);
        }
    }
}