/*
recast4j copyright (c) 2021 Piotr Piastucki piotr@jtilia.org
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
using static DotRecast.Recast.RcConstants;


namespace DotRecast.Recast
{
    

    /// <summary>
    /// 一个公共静态类，包含一组用于光栅化填充体积的方法。这些方法允许在高度场中光栅化各种形状，如球体、胶囊、圆柱体、立方体和凸多面体。在游戏和仿真中，这些方法通常用于生成导航网格或对场景中的物体进行碰撞检测
    /// 这个类的主要功能是将三维形状光栅化到二维高度场（通常用于导航网格生成或碰撞检测）。通过将形状投影到高度场上，可以更容易地进行导航和碰撞检测计算。这个类提供了一组通用的光栅化方法，可以处理各种常见的三维形状。
    /// </summary>
    public static class RcFilledVolumeRasterization
    {
        // 一个非常小的浮点数，用于避免浮点数精度问题
        private const float EPSILON = 0.00001f;
        // 一个整数数组，表示立方体的边缘索引
        private static readonly int[] BOX_EDGES = new[] { 0, 1, 0, 2, 0, 4, 1, 3, 1, 5, 2, 3, 2, 6, 3, 7, 4, 5, 4, 6, 5, 7, 6, 7 };

        // 光栅化一个球体，需要指定球体的中心、半径、区域、合并阈值和上下文。
        public static void RasterizeSphere(RcHeightfield hf, RcVec3f center, float radius, int area, int flagMergeThr, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_SPHERE);
            float[] bounds =
            {
                center.x - radius, center.y - radius, center.z - radius, center.x + radius, center.y + radius,
                center.z + radius
            };
            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectSphere(rectangle, center, radius * radius));
        }

        // 光栅化一个胶囊体，需要指定胶囊体的起点、终点、半径、区域、合并阈值和上下文。
        public static void RasterizeCapsule(RcHeightfield hf, RcVec3f start, RcVec3f end, float radius, int area, int flagMergeThr, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_CAPSULE);
            float[] bounds =
            {
                Math.Min(start.x, end.x) - radius, Math.Min(start.y, end.y) - radius,
                Math.Min(start.z, end.z) - radius, Math.Max(start.x, end.x) + radius, Math.Max(start.y, end.y) + radius,
                Math.Max(start.z, end.z) + radius
            };
            RcVec3f axis = RcVec3f.Of(end.x - start.x, end.y - start.y, end.z - start.z);
            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectCapsule(rectangle, start, end, axis, radius * radius));
        }

        // 光栅化一个圆柱体，需要指定圆柱体的起点、终点、半径、区域、合并阈值和上下文。
        public static void RasterizeCylinder(RcHeightfield hf, RcVec3f start, RcVec3f end, float radius, int area, int flagMergeThr, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_CYLINDER);
            float[] bounds =
            {
                Math.Min(start.x, end.x) - radius, Math.Min(start.y, end.y) - radius,
                Math.Min(start.z, end.z) - radius, Math.Max(start.x, end.x) + radius, Math.Max(start.y, end.y) + radius,
                Math.Max(start.z, end.z) + radius
            };
            RcVec3f axis = RcVec3f.Of(end.x - start.x, end.y - start.y, end.z - start.z);
            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectCylinder(rectangle, start, end, axis, radius * radius));
        }

        // 光栅化一个立方体，需要指定立方体的中心、半边、区域、合并阈值和上下文。
        public static void RasterizeBox(RcHeightfield hf, RcVec3f center, RcVec3f[] halfEdges, int area, int flagMergeThr, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_BOX);
            RcVec3f[] normals =
            {
                RcVec3f.Of(halfEdges[0].x, halfEdges[0].y, halfEdges[0].z),
                RcVec3f.Of(halfEdges[1].x, halfEdges[1].y, halfEdges[1].z),
                RcVec3f.Of(halfEdges[2].x, halfEdges[2].y, halfEdges[2].z),
            };
            RcVec3f.Normalize(ref normals[0]);
            RcVec3f.Normalize(ref normals[1]);
            RcVec3f.Normalize(ref normals[2]);

            float[] vertices = new float[8 * 3];
            float[] bounds = new float[]
            {
                float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,
                float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity
            };
            for (int i = 0; i < 8; ++i)
            {
                float s0 = (i & 1) != 0 ? 1f : -1f;
                float s1 = (i & 2) != 0 ? 1f : -1f;
                float s2 = (i & 4) != 0 ? 1f : -1f;
                vertices[i * 3 + 0] = center.x + s0 * halfEdges[0].x + s1 * halfEdges[1].x + s2 * halfEdges[2].x;
                vertices[i * 3 + 1] = center.y + s0 * halfEdges[0].y + s1 * halfEdges[1].y + s2 * halfEdges[2].y;
                vertices[i * 3 + 2] = center.z + s0 * halfEdges[0].z + s1 * halfEdges[1].z + s2 * halfEdges[2].z;
                bounds[0] = Math.Min(bounds[0], vertices[i * 3 + 0]);
                bounds[1] = Math.Min(bounds[1], vertices[i * 3 + 1]);
                bounds[2] = Math.Min(bounds[2], vertices[i * 3 + 2]);
                bounds[3] = Math.Max(bounds[3], vertices[i * 3 + 0]);
                bounds[4] = Math.Max(bounds[4], vertices[i * 3 + 1]);
                bounds[5] = Math.Max(bounds[5], vertices[i * 3 + 2]);
            }

            float[][] planes = RcArrayUtils.Of<float>(6, 4);
            for (int i = 0; i < 6; i++)
            {
                float m = i < 3 ? -1 : 1;
                int vi = i < 3 ? 0 : 7;
                planes[i][0] = m * normals[i % 3].x;
                planes[i][1] = m * normals[i % 3].y;
                planes[i][2] = m * normals[i % 3].z;
                planes[i][3] = vertices[vi * 3] * planes[i][0] + vertices[vi * 3 + 1] * planes[i][1]
                                                               + vertices[vi * 3 + 2] * planes[i][2];
            }

            RasterizationFilledShape(hf, bounds, area, flagMergeThr, rectangle => IntersectBox(rectangle, vertices, planes));
        }

        // 光栅化一个凸多面体，需要指定多面体的顶点、三角形、区域、合并阈值和上下文。
        public static void RasterizeConvex(RcHeightfield hf, float[] vertices, int[] triangles, int area, int flagMergeThr, RcTelemetry ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_CONVEX);
            float[] bounds = new float[] { vertices[0], vertices[1], vertices[2], vertices[0], vertices[1], vertices[2] };
            for (int i = 0; i < vertices.Length; i += 3)
            {
                bounds[0] = Math.Min(bounds[0], vertices[i + 0]);
                bounds[1] = Math.Min(bounds[1], vertices[i + 1]);
                bounds[2] = Math.Min(bounds[2], vertices[i + 2]);
                bounds[3] = Math.Max(bounds[3], vertices[i + 0]);
                bounds[4] = Math.Max(bounds[4], vertices[i + 1]);
                bounds[5] = Math.Max(bounds[5], vertices[i + 2]);
            }


            float[][] planes = RcArrayUtils.Of<float>(triangles.Length, 4);
            float[][] triBounds = RcArrayUtils.Of<float>(triangles.Length / 3, 4);
            for (int i = 0, j = 0; i < triangles.Length; i += 3, j++)
            {
                int a = triangles[i] * 3;
                int b = triangles[i + 1] * 3;
                int c = triangles[i + 2] * 3;
                float[] ab = { vertices[b] - vertices[a], vertices[b + 1] - vertices[a + 1], vertices[b + 2] - vertices[a + 2] };
                float[] ac = { vertices[c] - vertices[a], vertices[c + 1] - vertices[a + 1], vertices[c + 2] - vertices[a + 2] };
                float[] bc = { vertices[c] - vertices[b], vertices[c + 1] - vertices[b + 1], vertices[c + 2] - vertices[b + 2] };
                float[] ca = { vertices[a] - vertices[c], vertices[a + 1] - vertices[c + 1], vertices[a + 2] - vertices[c + 2] };
                Plane(planes, i, ab, ac, vertices, a);
                Plane(planes, i + 1, planes[i], bc, vertices, b);
                Plane(planes, i + 2, planes[i], ca, vertices, c);

                float s = 1.0f / (vertices[a] * planes[i + 1][0] + vertices[a + 1] * planes[i + 1][1]
                                                                 + vertices[a + 2] * planes[i + 1][2] - planes[i + 1][3]);
                planes[i + 1][0] *= s;
                planes[i + 1][1] *= s;
                planes[i + 1][2] *= s;
                planes[i + 1][3] *= s;

                s = 1.0f / (vertices[b] * planes[i + 2][0] + vertices[b + 1] * planes[i + 2][1] + vertices[b + 2] * planes[i + 2][2]
                            - planes[i + 2][3]);
                planes[i + 2][0] *= s;
                planes[i + 2][1] *= s;
                planes[i + 2][2] *= s;
                planes[i + 2][3] *= s;

                triBounds[j][0] = Math.Min(Math.Min(vertices[a], vertices[b]), vertices[c]);
                triBounds[j][1] = Math.Min(Math.Min(vertices[a + 2], vertices[b + 2]), vertices[c + 2]);
                triBounds[j][2] = Math.Max(Math.Max(vertices[a], vertices[b]), vertices[c]);
                triBounds[j][3] = Math.Max(Math.Max(vertices[a + 2], vertices[b + 2]), vertices[c + 2]);
            }

            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectConvex(rectangle, triangles, vertices, planes, triBounds));
        }

        // 用于计算一个平面的方程
        /*
         *  planes：一个二维浮点数数组，表示平面方程的系数。每个平面方程由4个浮点数表示，前三个数表示平面的法向量，第四个数表示平面的常数项。
            p：一个整数，表示要计算的平面方程在planes数组中的索引。
            v1和v2：两个三维向量，表示平面上的两个相邻边。
            vertices：一个浮点数数组，表示顶点坐标。
            vert：一个整数，表示要计算的平面上的一个顶点在vertices数组中的索引。
         */
        private static void Plane(float[][] planes, int p, float[] v1, float[] v2, float[] vertices, int vert)
        {
            // 方法首先计算平面的法向量，通过计算v1和v2的叉积得到
            RcVec3f.Cross(planes[p], v1, v2);
            // 然后计算平面方程的常数项，通过将法向量与一个平面上的顶点相乘得到。最后将计算得到的法向量和常数项存储在planes数组的相应位置。
            planes[p][3] = planes[p][0] * vertices[vert] + planes[p][1] * vertices[vert + 1] + planes[p][2] * vertices[vert + 2];
        }

        /// <summary>
        /// 用于光栅化填充形状.   这个方法是实现各种形状光栅化的核心，它将形状投影到高度场上，并根据交点更新高度场的数据。这样，可以在高度场中表示各种三维形状，从而方便进行导航和碰撞检测计算。
        /*
         *  hf：一个RcHeightfield对象，表示高度场。
            bounds：一个浮点数数组，表示形状的边界框。
            area：一个整数，表示形状所在的区域。
            flagMergeThr：一个整数，表示合并阈值。
            intersection：一个委托（Func），用于计算形状与高度场的交点。
         */
        private static void RasterizationFilledShape(RcHeightfield hf, float[] bounds, int area, int flagMergeThr,
            Func<float[], float[]> intersection)
        {
            // 方法首先检查形状的边界框是否与高度场的边界框重叠，如果不重叠则直接返回
            if (!OverlapBounds(hf.bmin, hf.bmax, bounds))
            {
                return;
            }

            // 接下来，根据高度场的边界和单元格大小，计算形状在高度场中的范围（xMin、xMax、zMin、zMax）
            bounds[3] = Math.Min(bounds[3], hf.bmax.x);
            bounds[5] = Math.Min(bounds[5], hf.bmax.z);
            bounds[0] = Math.Max(bounds[0], hf.bmin.x);
            bounds[2] = Math.Max(bounds[2], hf.bmin.z);

            if (bounds[3] <= bounds[0] || bounds[4] <= bounds[1] || bounds[5] <= bounds[2])
            {
                return;
            }

            float ics = 1.0f / hf.cs;
            float ich = 1.0f / hf.ch;
            int xMin = (int)((bounds[0] - hf.bmin.x) * ics);
            int zMin = (int)((bounds[2] - hf.bmin.z) * ics);
            int xMax = Math.Min(hf.width - 1, (int)((bounds[3] - hf.bmin.x) * ics));
            int zMax = Math.Min(hf.height - 1, (int)((bounds[5] - hf.bmin.z) * ics));
            float[] rectangle = new float[5];
            rectangle[4] = hf.bmin.y;
            // 然后遍历这个范围内的每个单元格，计算单元格与形状的交点，并将交点添加到高度场中
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    rectangle[0] = x * hf.cs + hf.bmin.x;
                    rectangle[1] = z * hf.cs + hf.bmin.z;
                    rectangle[2] = rectangle[0] + hf.cs;
                    rectangle[3] = rectangle[1] + hf.cs;
                    // 这里使用intersection委托来计算交点，以便根据具体的形状类型进行计算
                    float[] h = intersection.Invoke(rectangle);
                    if (h != null)
                    {
                        int smin = (int)Math.Floor((h[0] - hf.bmin.y) * ich);
                        int smax = (int)Math.Ceiling((h[1] - hf.bmin.y) * ich);
                        if (smin != smax)
                        {
                            int ismin = Math.Clamp(smin, 0, SPAN_MAX_HEIGHT);
                            int ismax = Math.Clamp(smax, ismin + 1, SPAN_MAX_HEIGHT);
                            RcRasterizations.AddSpan(hf, x, z, ismin, ismax, area, flagMergeThr);
                        }
                    }
                }
            }
        }

        // 用于计算球体与一个矩形的交点
        // 这个方法可以用于在球体光栅化过程中计算球体与高度场单元格的交点。通过将球体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         *  rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            center：一个RcVec3f对象，表示球体的中心。
            radiusSqr：一个浮点数，表示球体半径的平方。
         */
        private static float[] IntersectSphere(float[] rectangle, RcVec3f center, float radiusSqr)
        {
            // 方法首先计算矩形与球体中心的最近点（x, y, z），
            float x = Math.Max(rectangle[0], Math.Min(center.x, rectangle[2]));
            float y = rectangle[4];
            float z = Math.Max(rectangle[1], Math.Min(center.z, rectangle[3]));

            // 然后计算该点与球体中心的向量（mx, my, mz）。
            float mx = x - center.x;
            float my = y - center.y;
            float mz = z - center.z;

            // 接下来，计算二次方程的系数b和c，其中b是向量（mx, my, mz）与方向向量（0, 1, 0）的点积，
            float b = my; // Dot(m, d) d = (0, 1, 0)
            // c是向量（mx, my, mz）的长度平方减去球体半径的平方。
            float c = LenSqr(mx, my, mz) - radiusSqr;
            // 如果c大于0且b大于0，说明矩形与球体没有交点，返回null。
            if (c > 0.0f && b > 0.0f)
            {
                return null;
            }

            // 然后计算判别式（discr），如果判别式小于0，说明矩形与球体没有交点，返回null。
            float discr = b * b - c;
            if (discr < 0.0f)
            {
                return null;
            }

            // 接下来，计算判别式的平方根（discrSqrt），并计算交点的y坐标范围（tmin和tmax）。
            float discrSqrt = (float)Math.Sqrt(discr);
            float tmin = -b - discrSqrt;
            float tmax = -b + discrSqrt;

            // 如果tmin小于0，将其设置为0。
            if (tmin < 0.0f)
            {
                tmin = 0.0f;
            }

            // 最后返回一个包含交点y坐标范围的浮点数数组。
            return new float[] { y + tmin, y + tmax };
        }

        // 用于计算胶囊体与一个矩形的交点
        // 这个方法可以用于在胶囊体光栅化过程中计算胶囊体与高度场单元格的交点。通过将胶囊体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         *  rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            start：一个RcVec3f对象，表示胶囊体的起点。
            end：一个RcVec3f对象，表示胶囊体的终点。
            axis：一个RcVec3f对象，表示胶囊体的轴线。
            radiusSqr：一个浮点数，表示胶囊体半径的平方。
         */
        private static float[] IntersectCapsule(float[] rectangle, RcVec3f start, RcVec3f end, RcVec3f axis, float radiusSqr)
        {
            // 方法首先计算胶囊体两端球体与矩形的交点，并将这两个交点合并
            float[] s = MergeIntersections(IntersectSphere(rectangle, start, radiusSqr), IntersectSphere(rectangle, end, radiusSqr));
            // 接下来，计算轴线在二维平面（x, z）上的长度平方（axisLen2dSqr）
            float axisLen2dSqr = axis.x * axis.x + axis.z * axis.z;
            // 如果这个长度大于一个很小的值（EPSILON），说明胶囊体的轴线不与y轴平行，此时需要计算胶囊体中间圆柱体部分与矩形的交点，并将这个交点与之前合并的球体交点进行合并。
            if (axisLen2dSqr > EPSILON)
            {
                s = SlabsCylinderIntersection(rectangle, start, end, axis, radiusSqr, s);
            }

            // 最后返回一个包含交点y坐标范围的浮点数数组。
            return s;
        }

        //用于计算圆柱体与一个矩形的交点 
        // 这个方法可以用于在圆柱体光栅化过程中计算圆柱体与高度场单元格的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         *  rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            start：一个RcVec3f对象，表示圆柱体的起点。
            end：一个RcVec3f对象，表示圆柱体的终点。
            axis：一个RcVec3f对象，表示圆柱体的轴线。
            radiusSqr：一个浮点数，表示圆柱体半径的平方。
         */
        private static float[] IntersectCylinder(float[] rectangle, RcVec3f start, RcVec3f end, RcVec3f axis, float radiusSqr)
        {
            // 方法首先计算圆柱体侧面与矩形的交点，并将这两个交点合并
            float[] s = MergeIntersections(
                RayCylinderIntersection(RcVec3f.Of(
                    Math.Clamp(start.x, rectangle[0], rectangle[2]), rectangle[4],
                    Math.Clamp(start.z, rectangle[1], rectangle[3])
                ), start, axis, radiusSqr),
                RayCylinderIntersection(RcVec3f.Of(
                    Math.Clamp(end.x, rectangle[0], rectangle[2]), rectangle[4],
                    Math.Clamp(end.z, rectangle[1], rectangle[3])
                ), start, axis, radiusSqr));
            // 接下来，计算轴线在二维平面（x, z）上的长度平方（axisLen2dSqr）
            float axisLen2dSqr = axis.x * axis.x + axis.z * axis.z;
            // 如果这个长度大于一个很小的值（EPSILON），说明圆柱体的轴线不与y轴平行，此时需要计算圆柱体侧面与矩形的交点，并将这个交点与之前合并的交点进行合并。
            if (axisLen2dSqr > EPSILON)
            {
                s = SlabsCylinderIntersection(rectangle, start, end, axis, radiusSqr, s);
            }

            // 接下来，判断轴线在y轴上的投影是否大于EPSILON，如果大于，说明圆柱体的轴线与y轴不平行。
            // 此时需要计算圆柱体顶部和底部圆盖与矩形的交点，并将这些交点与之前合并的交点进行合并。
            if (axis.y * axis.y > EPSILON)
            {
                RcVec3f[] rectangleOnStartPlane = new RcVec3f[4];
                RcVec3f[] rectangleOnEndPlane = new RcVec3f[4];
                float ds = RcVec3f.Dot(axis, start);
                float de = RcVec3f.Dot(axis, end);
                for (int i = 0; i < 4; i++)
                {
                    float x = rectangle[(i + 1) & 2];
                    float z = rectangle[(i & 2) + 1];
                    RcVec3f a = RcVec3f.Of(x, rectangle[4], z);
                    float dotAxisA = RcVec3f.Dot(axis, a);
                    float t = (ds - dotAxisA) / axis.y;
                    rectangleOnStartPlane[i].x = x;
                    rectangleOnStartPlane[i].y = rectangle[4] + t;
                    rectangleOnStartPlane[i].z = z;
                    t = (de - dotAxisA) / axis.y;
                    rectangleOnEndPlane[i].x = x;
                    rectangleOnEndPlane[i].y = rectangle[4] + t;
                    rectangleOnEndPlane[i].z = z;
                }

                for (int i = 0; i < 4; i++)
                {
                    s = CylinderCapIntersection(start, radiusSqr, s, i, rectangleOnStartPlane);
                    s = CylinderCapIntersection(end, radiusSqr, s, i, rectangleOnEndPlane);
                }
            }

            // 最后返回一个包含交点y坐标范围的浮点数数组。
            return s;
        }

        // 用于计算圆柱体顶部或底部圆盖与矩形的交点
        // 这个方法可以用于在圆柱体光栅化过程中计算圆柱体顶部和底部圆盖与高度场单元格的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         *  start：一个RcVec3f对象，表示圆柱体顶部或底部圆盖的圆心。
            radiusSqr：一个浮点数，表示圆柱体半径的平方。
            s：一个浮点数数组，表示当前已知的交点y坐标范围。
            i：一个整数，表示矩形顶点在rectangleOnPlane数组中的索引。
            rectangleOnPlane：一个RcVec3f数组，表示矩形顶点在圆柱体顶部或底部平面上的投影。
         */
        private static float[] CylinderCapIntersection(RcVec3f start, float radiusSqr, float[] s, int i, RcVec3f[] rectangleOnPlane)
        {
            // 方法首先计算矩形顶点在圆柱体顶部或底部平面上的投影与圆心的向量m，以及矩形边的向量d。
            int j = (i + 1) % 4;
            // Ray against sphere intersection
            var m = RcVec3f.Of(
                rectangleOnPlane[i].x - start.x,
                rectangleOnPlane[i].y - start.y,
                rectangleOnPlane[i].z - start.z
            );
            var d = RcVec3f.Of(
                rectangleOnPlane[j].x - rectangleOnPlane[i].x,
                rectangleOnPlane[j].y - rectangleOnPlane[i].y,
                rectangleOnPlane[j].z - rectangleOnPlane[i].z
            );
            
            // 接下来，计算二次方程的系数b和c，其中b是向量m与向量d的点积除以向量d的长度平方，c是向量m的长度平方减去圆柱体半径的平方除以向量d的长度平方
            float dl = RcVec3f.Dot(d, d);
            float b = RcVec3f.Dot(m, d) / dl;
            float c = (RcVec3f.Dot(m, m) - radiusSqr) / dl;
            // 然后计算判别式（discr），如果判别式大于一个很小的值（EPSILON），说明矩形与圆盖有交点
            float discr = b * b - c;
            if (discr > EPSILON)
            {
                // 接下来，计算判别式的平方根（discrSqrt），并计算交点在矩形边上的参数范围（t1和t2）。如果t1小于等于1且t2大于等于0，说明矩形与圆盖有交点
                float discrSqrt = (float)Math.Sqrt(discr);
                float t1 = -b - discrSqrt;
                float t2 = -b + discrSqrt;
                if (t1 <= 1 && t2 >= 0)
                {
                    t1 = Math.Max(0, t1);
                    t2 = Math.Min(1, t2);
                    // 此时，计算交点的y坐标范围（y1和y2），并将这个范围与之前已知的交点范围进行合并。
                    float y1 = rectangleOnPlane[i].y + t1 * d.y;
                    float y2 = rectangleOnPlane[i].y + t2 * d.y;
                    float[] y = { Math.Min(y1, y2), Math.Max(y1, y2) };
                    s = MergeIntersections(s, y);
                }
            }

            // 最后返回一个包含交点y坐标范围的浮点数数组。
            return s;
        }

        // 用于计算圆柱体侧面与矩形的交点
        // 这个方法可以用于在圆柱体光栅化过程中计算圆柱体侧面与高度场单元格的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算
        /*
         *  rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            start：一个RcVec3f对象，表示圆柱体的起点。
            end：一个RcVec3f对象，表示圆柱体的终点。
            axis：一个RcVec3f对象，表示圆柱体的轴线。
            radiusSqr：一个浮点数，表示圆柱体半径的平方。
            s：一个浮点数数组，表示当前已知的交点y坐标范围。
         */
        private static float[] SlabsCylinderIntersection(float[] rectangle, RcVec3f start, RcVec3f end, RcVec3f axis, float radiusSqr, float[] s)
        {
            // 方法首先检查圆柱体的起点和终点是否在矩形的x轴和z轴范围之外
            // 如果在范围之外，分别调用XSlabCylinderIntersection和ZSlabCylinderIntersection方法计算圆柱体侧面与矩形在x轴和z轴方向的交点，并将这些交点与之前已知的交点范围进行合并。
            if (Math.Min(start.x, end.x) < rectangle[0])
            {
                s = MergeIntersections(s, XSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[0]));
            }

            if (Math.Max(start.x, end.x) > rectangle[2])
            {
                s = MergeIntersections(s, XSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[2]));
            }

            if (Math.Min(start.z, end.z) < rectangle[1])
            {
                s = MergeIntersections(s, ZSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[1]));
            }

            if (Math.Max(start.z, end.z) > rectangle[3])
            {
                s = MergeIntersections(s, ZSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[3]));
            }
            //最后返回一个包含交点y坐标范围的浮点数数组
            return s;
        }

        //用于计算圆柱体侧面与矩形在x轴方向的交点
        // 这个方法可以用于在圆柱体光栅化过程中计算圆柱体侧面与高度场单元格在x轴方向的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            start：一个RcVec3f对象，表示圆柱体的起点。
            axis：一个RcVec3f对象，表示圆柱体的轴线。
            radiusSqr：一个浮点数，表示圆柱体半径的平方。
            x：一个浮点数，表示矩形在x轴方向的边界值。
         */
        private static float[] XSlabCylinderIntersection(float[] rectangle, RcVec3f start, RcVec3f axis, float radiusSqr, float x)
        {
            // 方法首先调用XSlabRayIntersection方法计算矩形与圆柱体轴线在x轴方向的交点，然后调用RayCylinderIntersection方法计算圆柱体侧面与矩形在x轴方向的交点。
            // 最后返回一个包含交点y坐标范围的浮点数数组。
            return RayCylinderIntersection(XSlabRayIntersection(rectangle, start, axis, x), start, axis, radiusSqr);
        }

        //用于计算矩形与圆柱体轴线在x轴方向的交点
        // 这个方法可以用于在计算圆柱体侧面与高度场单元格在x轴方向的交点时，首先计算矩形与圆柱体轴线在x轴方向的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            start：一个RcVec3f对象，表示圆柱体的起点。
            direction：一个RcVec3f对象，表示圆柱体的轴线。
            x：一个浮点数，表示矩形在x轴方向的边界值。
         */
        private static RcVec3f XSlabRayIntersection(float[] rectangle, RcVec3f start, RcVec3f direction, float x)
        {
            // 方法首先计算交点在圆柱体轴线上的参数t，然后计算交点的z坐标，并将其限制在矩形的z轴范围内。最后返回一个表示交点坐标的RcVec3f对象。
            // 2d intersection of plane and segment
            float t = (x - start.x) / direction.x;
            float z = Math.Clamp(start.z + t * direction.z, rectangle[1], rectangle[3]);
            return RcVec3f.Of(x, rectangle[4], z);
        }

        // 用于计算圆柱体侧面与矩形在z轴方向的交点
        // 这个方法可以用于在圆柱体光栅化过程中计算圆柱体侧面与高度场单元格在z轴方向的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            start：一个RcVec3f对象，表示圆柱体的起点。
            axis：一个RcVec3f对象，表示圆柱体的轴线。
            radiusSqr：一个浮点数，表示圆柱体半径的平方。
            z：一个浮点数，表示矩形在z轴方向的边界值。
         */
        private static float[] ZSlabCylinderIntersection(float[] rectangle, RcVec3f start, RcVec3f axis, float radiusSqr, float z)
        {
            // 方法首先调用ZSlabRayIntersection方法计算矩形与圆柱体轴线在z轴方向的交点，然后调用RayCylinderIntersection方法计算圆柱体侧面与矩形在z轴方向的交点。
            //最后返回一个包含交点y坐标范围的浮点数数组
            return RayCylinderIntersection(ZSlabRayIntersection(rectangle, start, axis, z), start, axis, radiusSqr);
        }
        // 用于计算矩形与圆柱体轴线在z轴方向的交点
        // 这个方法可以用于在计算圆柱体侧面与高度场单元格在z轴方向的交点时，首先计算矩形与圆柱体轴线在z轴方向的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            start：一个RcVec3f对象，表示圆柱体的起点。
            direction：一个RcVec3f对象，表示圆柱体的轴线。
            z：一个浮点数，表示矩形在z轴方向的边界值。
         */
        private static RcVec3f ZSlabRayIntersection(float[] rectangle, RcVec3f start, RcVec3f direction, float z)
        {
            // 方法首先计算交点在圆柱体轴线上的参数t，然后计算交点的x坐标，并将其限制在矩形的x轴范围内。最后返回一个表示交点坐标的RcVec3f对象。
            // 2d intersection of plane and segment
            float t = (z - start.z) / direction.z;
            float x = Math.Clamp(start.x + t * direction.x, rectangle[0], rectangle[2]);
            return RcVec3f.Of(x, rectangle[4], z);
        }
        // 用于计算射线与圆柱体侧面的交点
        // 这个方法可以用于在计算圆柱体侧面与高度场单元格的交点时，首先计算射线与圆柱体侧面的交点。通过将圆柱体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         * point：一个RcVec3f对象，表示射线的起点。
            start：一个RcVec3f对象，表示圆柱体的起点。
            axis：一个RcVec3f对象，表示圆柱体的轴线。
            radiusSqr：一个浮点数，表示圆柱体半径的平方。
         */
        // Based on Christer Ericsons's "Real-Time Collision Detection" 基于 Christer Ericsons 的“实时碰撞检测”
        private static float[] RayCylinderIntersection(RcVec3f point, RcVec3f start, RcVec3f axis, float radiusSqr)
        {
            // 方法首先计算射线的方向向量d（即圆柱体的轴线）和射线起点到圆柱体起点的向量m。接下来，计算二次方程的系数a、b和c。
            // 然后判断a是否接近于0（使用一个很小的值EPSILON作为判断依据），如果接近于0，说明射线与圆柱体轴线平行。此时需要计算射线与圆柱体顶部和底部圆盖的交点。
            RcVec3f d = axis;
            RcVec3f m = RcVec3f.Of(point.x - start.x, point.y - start.y, point.z - start.z);
            // float[] n = { 0, 1, 0 };
            float md = RcVec3f.Dot(m, d);
            // float nd = Dot(n, d);
            float nd = axis.y;
            float dd = RcVec3f.Dot(d, d);

            // float nn = Dot(n, n);
            float nn = 1;
            // float mn = Dot(m, n);
            float mn = m.y;
            // float a = dd * nn - nd * nd;
            float a = dd - nd * nd;
            float k = RcVec3f.Dot(m, m) - radiusSqr;
            float c = dd * k - md * md;
            if (Math.Abs(a) < EPSILON)
            {
                // Segment runs parallel to cylinder axis
                if (c > 0.0f)
                {
                    return null; // ’a’ and thus the segment lie outside cylinder
                }

                // Now known that segment intersects cylinder; figure out how it intersects
                float tt1 = -mn / nn; // Intersect segment against ’p’ endcap
                float tt2 = (nd - mn) / nn; // Intersect segment against ’q’ endcap
                return new float[] { point.y + Math.Min(tt1, tt2), point.y + Math.Max(tt1, tt2) };
            }

            // 如果a不接近于0，计算判别式（discr）。如果判别式小于0，说明没有实根，即没有交点。否则，计算判别式的平方根（discSqrt）和交点在射线上的参数范围（t1和t2）。
            float b = dd * mn - nd * md;
            float discr = b * b - a * c;
            if (discr < 0.0f)
            {
                return null; // No real roots; no intersection
            }

            float discSqrt = (float)Math.Sqrt(discr);
            float t1 = (-b - discSqrt) / a;
            float t2 = (-b + discSqrt) / a;
            
            //接下来，判断交点是否在圆柱体的范围内。如果交点在圆柱体的顶部和底部圆盖之外，返回null。否则，返回一个包含交点y坐标范围的浮点数数组。
            if (md + t1 * nd < 0.0f)
            {
                // Intersection outside cylinder on ’p’ side
                t1 = -md / nd;
                if (k + t1 * (2 * mn + t1 * nn) > 0.0f)
                {
                    return null;
                }
            }
            else if (md + t1 * nd > dd)
            {
                // Intersection outside cylinder on ’q’ side
                t1 = (dd - md) / nd;
                if (k + dd - 2 * md + t1 * (2 * (mn - nd) + t1 * nn) > 0.0f)
                {
                    return null;
                }
            }

            if (md + t2 * nd < 0.0f)
            {
                // Intersection outside cylinder on ’p’ side
                t2 = -md / nd;
                if (k + t2 * (2 * mn + t2 * nn) > 0.0f)
                {
                    return null;
                }
            }
            else if (md + t2 * nd > dd)
            {
                // Intersection outside cylinder on ’q’ side
                t2 = (dd - md) / nd;
                if (k + dd - 2 * md + t2 * (2 * (mn - nd) + t2 * nn) > 0.0f)
                {
                    return null;
                }
            }

            return new float[] { point.y + Math.Min(t1, t2), point.y + Math.Max(t1, t2) };
        }
        // 用于计算一个矩形与一个立方体的交点
        // 这个方法可以用于在计算立方体与高度场单元格的交点时，首先计算立方体与高度场上的矩形的交点。通过将立方体投影到高度场上，可以更容易地进行导航和碰撞检测计算
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            vertices：一个浮点数数组，表示立方体的顶点坐标。数组中的元素依次表示每个顶点的x、y、z坐标。
            planes：一个浮点数二维数组，表示立方体的六个面。每个面由一个长度为4的浮点数数组表示，依次表示面的法线x分量、法线y分量、法线z分量和面的距离。
         */
        private static float[] IntersectBox(float[] rectangle, float[] vertices, float[][] planes)
        {
            // 方法首先检查立方体顶点是否在矩形内。如果在矩形内，更新交点的y坐标范围（yMin和yMax）。
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;
            // check intersection with rays starting in box vertices first 哎呀与首先从盒子顶点开始的光线相交
            for (int i = 0; i < 8; i++)
            {
                int vi = i * 3;
                if (vertices[vi] >= rectangle[0] && vertices[vi] < rectangle[2] && vertices[vi + 2] >= rectangle[1]
                    && vertices[vi + 2] < rectangle[3])
                {
                    yMin = Math.Min(yMin, vertices[vi + 1]);
                    yMax = Math.Max(yMax, vertices[vi + 1]);
                }
            }

            // 接下来，检查矩形顶点是否在立方体内。如果在立方体内，更新交点的y坐标范围。
            // check intersection with rays starting in rectangle vertices检查与从矩形顶点开始的射线的交集
            var point = RcVec3f.Of(0, rectangle[1], 0);
            for (int i = 0; i < 4; i++)
            {
                point.x = ((i & 1) == 0) ? rectangle[0] : rectangle[2];
                point.z = ((i & 2) == 0) ? rectangle[1] : rectangle[3];
                for (int j = 0; j < 6; j++)
                {
                    if (Math.Abs(planes[j][1]) > EPSILON)
                    {
                        float dotNormalPoint = RcVec3f.Dot(planes[j], point);
                        float t = (planes[j][3] - dotNormalPoint) / planes[j][1];
                        float y = point.y + t;
                        bool valid = true;
                        for (int k = 0; k < 6; k++)
                        {
                            if (k != j)
                            {
                                if (point.x * planes[k][0] + y * planes[k][1] + point.z * planes[k][2] > planes[k][3])
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }

                        if (valid)
                        {
                            yMin = Math.Min(yMin, y);
                            yMax = Math.Max(yMax, y);
                        }
                    }
                }
            }

            // 然后，检查立方体边与矩形的交点。对于立方体的每条边，计算与矩形在x轴和z轴方向的交点，并更新交点的y坐标范围。
            // check intersection with box edges检查与框边缘的交集
            for (int i = 0; i < BOX_EDGES.Length; i += 2)
            {
                int vi = BOX_EDGES[i] * 3;
                int vj = BOX_EDGES[i + 1] * 3;
                float x = vertices[vi];
                float z = vertices[vi + 2];
                // edge slab intersection边板相交
                float y = vertices[vi + 1];
                float dx = vertices[vj] - x;
                float dy = vertices[vj + 1] - y;
                float dz = vertices[vj + 2] - z;
                if (Math.Abs(dx) > EPSILON)
                {
                    if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[0], out var iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }

                    if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[2], out iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }
                }

                if (Math.Abs(dz) > EPSILON)
                {
                    if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[1], out var iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }

                    if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[3], out iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }
                }
            }

            // 最后，如果yMin小于等于yMax，返回一个包含交点y坐标范围的浮点数数组。否则，返回null，表示没有交点。
            if (yMin <= yMax)
            {
                return new float[] { yMin, yMax };
            }

            return null;
        }
        // 用于计算一个矩形与一个凸多面体的交点
        // 这个方法可以用于在计算凸多面体与高度场单元格的交点时，首先计算凸多面体与高度场上的矩形的交点。通过将凸多面体投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
triangles：一个整数数组，表示凸多面体的三角形索引。数组中的每3个元素表示一个三角形的3个顶点在verts数组中的索引。
verts：一个浮点数数组，表示凸多面体的顶点坐标。数组中的元素依次表示每个顶点的x、y、z坐标。
planes：一个浮点数二维数组，表示凸多面体的面。每个面由一个长度为4的浮点数数组表示，依次表示面的法线x分量、法线y分量、法线z分量和面的距离。
triBounds：一个浮点数二维数组，表示凸多面体的三角形边界。每个三角形边界由一个长度为4的浮点数数组表示，依次表示三角形的左下角x坐标、左下角z坐标、右上角x坐标和右上角z坐标。
         */
        private static float[] IntersectConvex(float[] rectangle, int[] triangles, float[] verts, float[][] planes,
            float[][] triBounds)
        {
            // 方法首先初始化交点的y坐标范围（imin和imax）。然后遍历凸多面体的每个三角形，检查三角形是否与矩形相交。
            float imin = float.PositiveInfinity;
            float imax = float.NegativeInfinity;
            for (int tr = 0, tri = 0; tri < triangles.Length; tr++, tri += 3)
            {
                // 对于每个相交的三角形，检查三角形顶点是否在矩形内。如果在矩形内，更新交点的y坐标范围。接着，检查三角形边与矩形在x轴和z轴方向的交点，并更新交点的y坐标范围。
                if (triBounds[tr][0] > rectangle[2] || triBounds[tr][2] < rectangle[0] || triBounds[tr][1] > rectangle[3]
                    || triBounds[tr][3] < rectangle[1])
                {
                    continue;
                }

                if (Math.Abs(planes[tri][1]) < EPSILON)
                {
                    continue;
                }

                for (int i = 0; i < 3; i++)
                {
                    int vi = triangles[tri + i] * 3;
                    int vj = triangles[tri + (i + 1) % 3] * 3;
                    float x = verts[vi];
                    float z = verts[vi + 2];
                    // triangle vertex  三角形顶点
                    if (x >= rectangle[0] && x <= rectangle[2] && z >= rectangle[1] && z <= rectangle[3])
                    {
                        imin = Math.Min(imin, verts[vi + 1]);
                        imax = Math.Max(imax, verts[vi + 1]);
                    }

                    // triangle slab intersection  三角形板相交
                    float y = verts[vi + 1];
                    float dx = verts[vj] - x;
                    float dy = verts[vj + 1] - y;
                    float dz = verts[vj + 2] - z;
                    if (Math.Abs(dx) > EPSILON)
                    {
                        if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[0], out var iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }

                        if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[2], out iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }
                    }

                    if (Math.Abs(dz) > EPSILON)
                    {
                        if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[1], out var iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }

                        if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[3], out iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }
                    }
                }

                // 然后，检查矩形顶点是否在凸多面体内。如果在凸多面体内，更新交点的y坐标范围。
                // rectangle vertex  矩形顶点
                var point = RcVec3f.Of(0, rectangle[1], 0);
                for (int i = 0; i < 4; i++)
                {
                    point.x = ((i & 1) == 0) ? rectangle[0] : rectangle[2];
                    point.z = ((i & 2) == 0) ? rectangle[1] : rectangle[3];
                    if (RayTriangleIntersection(point, tri, planes, out var y))
                    {
                        imin = Math.Min(imin, y);
                        imax = Math.Max(imax, y);
                    }
                }
            }

            // 最后，如果imin小于imax，返回一个包含交点y坐标范围的浮点数数组。否则，返回null，表示没有交点。
            if (imin < imax)
            {
                return new float[] { imin, imax };
            }

            return null;
        }
        // 用于计算线段与矩形在x轴方向的交点
        // 这个方法可以用于在计算线段与高度场单元格的交点时，首先计算线段与高度场上的矩形在x轴方向的交点。通过将线段投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            x、y、z：浮点数，表示线段起点的x、y、z坐标。
            dx、dy、dz：浮点数，表示线段的方向向量的x、y、z分量。
            slabX：一个浮点数，表示矩形在x轴方向的边界值。
            iy：一个输出参数，用于返回交点的y坐标。
         */
        private static bool XSlabSegmentIntersection(float[] rectangle, float x, float y, float z, float dx, float dy, float dz, float slabX, out float iy)
        {
            // 方法首先检查线段是否与矩形在x轴方向的边界值相交。
            // 如果相交，计算交点在线段上的参数t，然后计算交点的z坐标。
            // 如果交点的z坐标在矩形的z轴范围内，计算交点的y坐标并返回true。否则，返回false。
            float x2 = x + dx;
            if ((x < slabX && x2 > slabX) || (x > slabX && x2 < slabX))
            {
                float t = (slabX - x) / dx;
                float iz = z + dz * t;
                if (iz >= rectangle[1] && iz <= rectangle[3])
                {
                    iy = y + dy * t;
                    return true;
                }
            }

            iy = 0.0f;
            return false;
        }
        // 用于计算线段与矩形在z轴方向的交点
        // 这个方法可以用于在计算线段与高度场单元格的交点时，首先计算线段与高度场上的矩形在z轴方向的交点。通过将线段投影到高度场上，可以更容易地进行导航和碰撞检测计算。
        /*
         * rectangle：一个浮点数数组，表示矩形的边界。数组中的元素依次表示矩形的左下角x坐标、左下角z坐标、右上角x坐标、右上角z坐标和矩形所在的y坐标。
            x、y、z：浮点数，表示线段起点的x、y、z坐标。
            dx、dy、dz：浮点数，表示线段的方向向量的x、y、z分量。
            slabZ：一个浮点数，表示矩形在z轴方向的边界值。
            iy：一个输出参数，用于返回交点的y坐标。
         */
        private static bool ZSlabSegmentIntersection(float[] rectangle, float x, float y, float z, float dx, float dy, float dz, float slabZ, out float iy)
        {
            // 方法首先检查线段是否与矩形在z轴方向的边界值相交。
            // 如果相交，计算交点在线段上的参数t，然后计算交点的x坐标。
            // 如果交点的x坐标在矩形的x轴范围内，计算交点的y坐标并返回true。否则，返回false。
            float z2 = z + dz;
            if ((z < slabZ && z2 > slabZ) || (z > slabZ && z2 < slabZ))
            {
                float t = (slabZ - z) / dz;
                float ix = x + dx * t;
                if (ix >= rectangle[0] && ix <= rectangle[2])
                {
                    iy = y + dy * t;
                    return true;
                }
            }

            iy = 0.0f;
            return false;
        }
        // 用于计算射线与三角形的交点
        // 这个方法可以用于在计算射线与三角形的交点时，首先判断射线是否与三角形所在的平面相交。通过将射线投影到三角形所在的平面上，可以更容易地进行导航和碰撞检测计算。
        /*
         * point：一个RcVec3f对象，表示射线的起点。
            plane：一个整数，表示三角形所在平面在planes数组中的索引。
            planes：一个浮点数二维数组，表示三角形的平面。每个平面由一个长度为4的浮点数数组表示，依次表示平面的法线x分量、法线y分量、法线z分量和平面的距离。
            y：一个输出参数，用于返回交点的y坐标。
         */
        private static bool RayTriangleIntersection(RcVec3f point, int plane, float[][] planes, out float y)
        {
            //方法首先计算交点在线段上的参数t，然后计算交点的三角形重心坐标（u、v、w）。
            //如果u、v、w都在[0, 1]范围内，说明交点在三角形内，返回true并设置交点的y坐标。否则，返回false。
            y = 0.0f;
            float t = (planes[plane][3] - RcVec3f.Dot(planes[plane], point)) / planes[plane][1];
            float[] s = { point.x, point.y + t, point.z };
            float u = RcVec3f.Dot(s, planes[plane + 1]) - planes[plane + 1][3];
            if (u < 0.0f || u > 1.0f)
            {
                return false;
            }

            float v = RcVec3f.Dot(s, planes[plane + 2]) - planes[plane + 2][3];
            if (v < 0.0f)
            {
                return false;
            }

            float w = 1f - u - v;
            if (w < 0.0f)
            {
                return false;
            }

            y = s[1];
            return true;
        }
        // 用于合并两个交点区间
        // 这个方法可以用于在计算多个几何体与高度场单元格的交点时，将各个几何体的交点区间合并成一个总的交点区间。通过将多个交点区间合并，可以更容易地进行导航和碰撞检测计算。
        /*
         * s1：一个浮点数数组，表示第一个交点区间。数组中的元素依次表示交点区间的最小y坐标和最大y坐标。
            s2：一个浮点数数组，表示第二个交点区间。数组中的元素依次表示交点区间的最小y坐标和最大y坐标。
         */
        private static float[] MergeIntersections(float[] s1, float[] s2)
        {
            //方法首先检查s1和s2是否为null。如果其中一个为null，返回另一个非null的区间。
            //如果两个都不为null，返回一个新的浮点数数组，表示合并后的交点区间，其最小y坐标为s1和s2的最小y坐标中的较小值，最大y坐标为s1和s2的最大y坐标中的较大值。
            if (s1 == null)
            {
                return s2;
            }

            if (s2 == null)
            {
                return s1;
            }

            return new float[] { Math.Min(s1[0], s2[0]), Math.Max(s1[1], s2[1]) };
        }
        // 用于计算一个三维向量的长度平方
        // 这个方法可以用于在计算向量长度时避免开方运算，从而提高计算性能。
        // 例如，在计算两点之间的距离时，可以先计算距离的平方，然后与阈值的平方进行比较，从而避免进行开方运算。
        /*
         * dx：一个浮点数，表示向量的x分量。
            dy：一个浮点数，表示向量的y分量。
            dz：一个浮点数，表示向量的z分量。
         */
        private static float LenSqr(float dx, float dy, float dz)
        {
            return dx * dx + dy * dy + dz * dz;
        }
        // 用于检查两个边界框是否重叠
        // 这个方法可以用于在进行碰撞检测时，快速判断两个边界框是否有可能发生碰撞。通过先检查边界框的重叠，可以减少不必要的详细碰撞检测计算。
        /*
         * amin：一个RcVec3f对象，表示第一个边界框的最小顶点坐标（即左下角顶点）。
            amax：一个RcVec3f对象，表示第一个边界框的最大顶点坐标（即右上角顶点）。
            bounds：一个浮点数数组，表示第二个边界框的边界。数组中的元素依次表示边界框的最小x坐标、最小z坐标、最大x坐标、最大z坐标和最大y坐标。
         */
        private static bool OverlapBounds(RcVec3f amin, RcVec3f amax, float[] bounds)
        {
            //方法首先初始化一个布尔变量overlap为true。
            //然后分别检查两个边界框在x、y、z轴上是否有重叠。如果在某个轴上没有重叠，将overlap设置为false。最后返回overlap。
            bool overlap = true;
            overlap = (amin.x > bounds[3] || amax.x < bounds[0]) ? false : overlap;
            overlap = (amin.y > bounds[4]) ? false : overlap;
            overlap = (amin.z > bounds[5] || amax.z < bounds[2]) ? false : overlap;
            return overlap;
        }
    }
}