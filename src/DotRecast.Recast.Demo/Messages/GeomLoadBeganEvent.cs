namespace DotRecast.Recast.Demo.Messages;

/// <summary>
/// GeomLoadBeganEvent类实现了IRecastDemoMessage接口，表示一个与RecastDemo应用程序相关的消息。在这个例子中，GeomLoadBeganEvent表示一个几何体加载开始的事件。这个类包含一个FilePath属性，表示加载几何体的文件路径。
/// </summary>
public class GeomLoadBeganEvent : IRecastDemoMessage
{
    // 注意，这里使用了C# 9.0的新特性，如init访问器和required关键字。init访问器使得属性在对象初始化时可写，但在初始化后变为只读。required关键字表示该属性在对象初始化时必须被赋值。
    public required string FilePath { get; init; }
}