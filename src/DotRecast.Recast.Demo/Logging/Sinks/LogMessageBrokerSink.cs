using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace DotRecast.Recast.Demo.Logging.Sinks;

/// <summary>
/// 它实现了ILogEventSink接口。这个类用于自定义日志消息的输出方式。在这个例子中，它将日志消息通过一个事件OnEmitted广播出去，允许其他类订阅这个事件并处理这些日志消息。
/// </summary>
public class LogMessageBrokerSink : ILogEventSink
{
    // 当日志消息被输出时触发的事件。事件的参数包括日志级别和格式化后的日志消息字符串。
    public static event Action<int, string> OnEmitted;
    // 一个ITextFormatter接口的实例，用于格式化日志消息。
    private readonly ITextFormatter _formatter;

    public LogMessageBrokerSink(ITextFormatter formatter)
    {
        _formatter = formatter;
    }
    /// <summary>
    /// 实现ILogEventSink接口的方法，用于处理日志消息。这个方法首先使用_formatter对日志事件进行格式化，然后将格式化后的字符串通过OnEmitted事件广播出去。
    /// </summary>
    /// <param name="logEvent"></param>
    /// <returns></returns>
    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        OnEmitted?.Invoke((int)logEvent.Level, writer.ToString());
    }
}