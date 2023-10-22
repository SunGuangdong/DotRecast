namespace DotRecast.Recast.Demo;

/// <summary>
/// 它包含了一组常量，表示键盘上不同功能键的状态。
/// </summary>
public static class KeyModState
{
    /*
     * None：表示没有按下任何功能键。
        Shift：表示按下了Shift键。
        Control：表示按下了Control键。
        Alt：表示按下了Alt键。
        Super：表示按下了Super键（在Windows和Linux系统上通常是Win键，在macOS系统上是Command键）。
        CapsLock：表示CapsLock键处于激活状态。
        NumLock：表示NumLock键处于激活状态。
     */
    public const int None = 0;
    public const int Shift = 1;
    public const int Control = 2;
    public const int Alt = 4;
    public const int Super = 8;
    public const int CapsLock = 16;
    public const int NumLock = 32;
}