using System;
using DotRecast.Core;
using DotRecast.Detour.Dynamic.Io;
using K4os.Compression.LZ4;

namespace DotRecast.Recast.Demo;

/// <summary>
/// 它实现了IRcCompressor接口。这个类用于压缩和解压缩导航网格中的体素瓦片数据。它使用LZ4压缩算法进行压缩和解压缩操作。以下是类中的字段和方法的详细解释：
/// </summary>
public class DtVoxelTileLZ4DemoCompressor : IRcCompressor
{
    // 这是一个静态只读字段，存储一个DtVoxelTileLZ4DemoCompressor对象的实例，用于在其他地方共享和使用。
    public static readonly DtVoxelTileLZ4DemoCompressor Shared = new();

    private DtVoxelTileLZ4DemoCompressor()
    {
    }

    //此方法用于解压缩给定的字节数组。它首先从字节数组中获取压缩数据的大小，然后使用LZ4Pickler.Unpickle()方法对数据进行解压缩。
    public byte[] Decompress(byte[] data)
    {
        int compressedSize = RcByteUtils.GetIntBE(data, 0);
        return LZ4Pickler.Unpickle(data.AsSpan(4, compressedSize));
    }

    // 此方法用于从给定的字节数组、偏移量和长度解压缩数据。它使用LZ4Pickler.Unpickle()方法对数据进行解压缩。
    public byte[] Decompress(byte[] buf, int offset, int len, int outputlen)
    {
        return LZ4Pickler.Unpickle(buf, offset, len);
    }
    // ：此方法用于压缩给定的字节数组。它首先使用LZ4Pickler.Pickle()方法对数据进行压缩，然后将压缩后的数据复制到新的字节数组，并在新字节数组的开头添加压缩数据的长度。最后返回压缩后的字节数组。
    public byte[] Compress(byte[] data)
    {
        byte[] compressed = LZ4Pickler.Pickle(data, LZ4Level.L12_MAX);
        byte[] result = new byte[4 + compressed.Length];
        RcByteUtils.PutInt(compressed.Length, result, 0, RcByteOrder.BIG_ENDIAN);
        Array.Copy(compressed, 0, result, 4, compressed.Length);
        return result;
    }
}