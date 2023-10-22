namespace DotRecast.Recast.Demo.Messages;


/// <summary>
/// IRecastDemoChannel是一个接口，它定义了一个用于发送IRecastDemoMessage类型消息的通道。这个接口包含一个名为SendMessage的方法，该方法接受一个IRecastDemoMessage类型的参数。
/// 为了实现这个接口，你需要创建一个类，该类实现了IRecastDemoChannel接口，并提供了SendMessage方法的具体实现。例如，你可以创建一个类，该类将IRecastDemoMessage消息发送到远程服务器，或者在本地存储这些消息。
/// </summary>
public interface IRecastDemoChannel
{
    void SendMessage(IRecastDemoMessage message);
}