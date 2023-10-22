using System;

namespace DotRecast.Recast.Demo.Draw;

/// <summary>
/// 用于存储和管理可变大小的数组。它提供了添加元素、清除元素和获取内部数组的方法。
/// </summary>
/// <typeparam name="T"></typeparam>
public class ArrayBuffer<T>
{
    private int _size;
    private T[] _items;
    public int Count => _size;

    public ArrayBuffer()
    {
        // 初始化一个空的ArrayBuffer实例，将_size设置为0，并将_items设置为一个空数组。
        _size = 0;
        _items = Array.Empty<T>();
    }

    /// <summary>
    /// 向ArrayBuffer中添加一个元素。首先检查_items数组的长度是否足够容纳新元素，如果长度为0，则创建一个新的数组，初始大小为256。
    /// 如果_items数组的长度小于或等于_size（表示数组已满），则创建一个新的数组，大小为当前_size的1.5倍，并将_items数组中的元素复制到新数组中。然后将新元素添加到_items数组中，并更新_size。
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public void Add(T item)
    {
        if (0 >= _items.Length)
        {
            _items = new T[256];
        }

        if (_items.Length <= _size)
        {
            var temp = new T[(int)(_size * 1.5)];
            Array.Copy(_items, 0, temp, 0, _items.Length);
            _items = temp;
        }

        _items[_size++] = item;
    }

    /// <summary>
    /// 清除ArrayBuffer中的所有元素，将_size设置为0。
    /// </summary>
    /// <returns></returns>
    public void Clear()
    {
        _size = 0;
    }

    public T[] AsArray()
    {
        return _items;
    }
}