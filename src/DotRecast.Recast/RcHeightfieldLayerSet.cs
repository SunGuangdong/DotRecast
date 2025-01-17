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

using DotRecast.Core;

namespace DotRecast.Recast
{
    /// Represents a set of heightfield layers.
    /// @ingroup recast
    /// @see rcAllocHeightfieldLayerSet, rcFreeHeightfieldLayerSet
    /// 表示一组高度场层。这个类通常用于在构建导航网格时存储地形高度信息的多个子集。类中包含以下属性：
    /// 个类用于在导航网格构建过程中存储地形高度信息的多个子集。通过使用高度场层集，可以将地形划分为多个子集，从而更容易地进行导航和碰撞检测计算。
    /// RcHeightfieldLayerSet类可与RcHeightfieldLayer类结合使用，以表示和操作高度场层的集合。
    public class RcHeightfieldLayerSet
    {
        //layers：一个RcHeightfieldLayer数组，表示集合中的各个高度场层。
        public RcHeightfieldLayer[] layers; // < The layers in the set. [Size: #nlayers]
    }
}