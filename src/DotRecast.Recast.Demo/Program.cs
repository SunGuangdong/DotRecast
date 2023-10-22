using System.IO;
using DotRecast.Core;
using DotRecast.Recast.Demo.Logging.Sinks;
using Serilog;
using Serilog.Enrichers;

namespace DotRecast.Recast.Demo;

public static class Program
{
    public static void Main(string[] args)
    {
        InitializeWorkingDirectory();
        InitializeLogger();
        StartDemo();
    }

/// <summary>
/// 这个方法用于初始化日志记录器。它使用Serilog库创建一个新的日志记录器，并配置了不同的输出接收器。
/// 输出模板定义了日志消息的格式。日志记录器将异步地将日志消息发送到以下接收器：
/// </summary>
/// <returns></returns>
    private static void InitializeLogger()
    {
        var format = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} [{ThreadName}:{ThreadId}]{NewLine}{Exception}";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.WithProperty(ThreadNameEnricher.ThreadNamePropertyName, "main")
            .WriteTo.Async(c => c.LogMessageBroker(outputTemplate: format))
            .WriteTo.Async(c => c.Console(outputTemplate: format))
            .WriteTo.Async(c => c.File(
                "logs/log.log",
                rollingInterval: RollingInterval.Hour,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: null,
                outputTemplate: format)
            )
            .CreateLogger();
    }
    /// <summary>
    /// 这个方法用于设置当前工作目录。程序首先在"resources/dungeon.obj"文件中搜索，如果找到该文件，它将设置工作目录为包含该文件的目录。
    /// </summary>
    /// <returns></returns>
    private static void InitializeWorkingDirectory()
    {
        var path = RcDirectory.SearchDirectory("resources/dungeon.obj");
        if (!string.IsNullOrEmpty(path))
        {
            var workingDirectory = Path.GetDirectoryName(path) ?? string.Empty;
            workingDirectory = Path.GetFullPath(workingDirectory);
            Directory.SetCurrentDirectory(workingDirectory);
        }
    }

    private static void StartDemo()
    {
        var demo = new RecastDemo();
        demo.Run();
    }
}