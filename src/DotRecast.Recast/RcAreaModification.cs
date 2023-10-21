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

namespace DotRecast.Recast
{
    /// <summary>
    /// 表示一个区域修改（Area Modification），它用于在导航网格中修改区域ID。
    /// 这个类通常用于在导航网格的构建过程中，对特定区域进行修改，例如改变区域的类型、属性等。
    /// 注： SampleAreaModifications 中有预定义flag, 就是类似Unity的area枚举
    /// </summary>
    public class RcAreaModification
    {
        /// <summary>
        /// 表示可用的区域标志掩码，用于限制区域ID的范围。
        /// </summary>
        public const int RC_AREA_FLAGS_MASK = 0x3F;    // 0011 1111

        /// <summary>
        /// 表示要应用的区域ID。
        /// </summary>
        public readonly int Value;
        /// <summary>
        /// 表示应用区域ID时使用的位掩码。
        /// </summary>
        public readonly int Mask;

        /**
         * Mask is set to all available bits, which means value is fully applied
         *
         * @param value
         *            The area id to apply. [Limit: &lt;= #RC_AREA_FLAGS_MASK]
         */
        // 构造函数1：接收一个区域ID作为参数，设置掩码为所有可用位。这意味着区域ID将完全应用。
        public RcAreaModification(int value)
        {
            Value = value;
            Mask = RC_AREA_FLAGS_MASK;
        }

        /**
         *
         * @param value
         *            The area id to apply. [Limit: &lt;= #RC_AREA_FLAGS_MASK]
         * @param mask
         *            Bitwise mask used when applying value. [Limit: &lt;= #RC_AREA_FLAGS_MASK]
         */
        // 构造函数2：接收一个区域ID和一个位掩码作为参数，用于在应用区域ID时进行位运算。
        public RcAreaModification(int value, int mask)
        {
            Value = value;
            Mask = mask;
        }

        /// <summary>
        /// 构造函数3：接收另一个RcAreaModification实例作为参数，将其值和掩码复制到新实例
        /// </summary>
        /// <param name="other"></param>
        public RcAreaModification(RcAreaModification other)
        {
            Value = other.Value;
            Mask = other.Mask;
        }

        /// <summary>
        /// 返回经过掩码处理后的区域ID。
        /// </summary>
        /// <returns></returns>
        public int GetMaskedValue()
        {
            return Value & Mask;
        }

        /// <summary>
        /// 接收一个原始区域ID作为参数，将当前实例的区域ID和掩码应用到原始区域ID上，并返回修改后的区域ID。
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public int Apply(int area)
        {
            // Value & Mask。这将保留Value中与Mask相对应的位，而将其他位设置为0。
            // area & ~Mask。这将保留area中与Mask相对应的位之外的位，而将与Mask相对应的位设置为0。
            // 将合并步骤1和步骤2的结果，使得输出的区域ID中与Mask相对应的位来自Value，而其他位来自输入的area。
            return ((Value & Mask) | (area & ~Mask));
        }
    }
}