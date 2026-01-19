using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WinDiagMcpServer;

#pragma warning disable SA1201 // A enum should not follow a struct

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable IdentifierTypo
// ReSharper disable ArrangeTypeMemberModifiers
internal static class Win32Api
{
    private const uint GrGdiObjects = 0;

    private const uint GrUserObjects = 1;

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static (uint GdiObjects, uint UserObjects) GetGuiResourcesCounts(Process process)
    {
        try
        {
            IntPtr processHandle = process.Handle;
            uint gdiObjects = GetGuiResources(processHandle, GrGdiObjects);
            uint userObjects = GetGuiResources(processHandle, GrUserObjects);
            return (gdiObjects, userObjects);
        }
        catch (Exception)
        {
            return (0, 0);
        }
    }

    public static string GetClassName(IntPtr windowHandle)
    {
        var className = new StringBuilder(256);
        GetClassName(windowHandle, className, className.Capacity);
        return className.ToString();
    }

    public static string GetWindowBounds(nint windowHandle)
    {
        return GetWindowRect(windowHandle, out var rect)
            ? $"Left: {rect.Left}, Top: {rect.Top}, Width: {rect.Right - rect.Left}, Height: {rect.Bottom - rect.Top}"
            : "Error retrieving window bounds";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(SnapshotOptions flags, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32First(IntPtr snapshotHandle, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32Next(IntPtr snapshotHandle, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr windowHandle);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr windowHandle, StringBuilder value, int maxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetGuiResources(IntPtr processHandle, uint flag);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetClassName(IntPtr windowHandle, StringBuilder className, int maxCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;
    }

    [Flags]
    public enum SnapshotOptions : uint
    {
        HeapList = 0x00000001,
        Process = 0x00000002,
        Thread = 0x00000004,
        Module = 0x00000008,
        Module32 = 0x00000010,
        Inherit = 0x80000000,
        All = 0x0000001F,
        NoHeaps = 0x40000000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessEntry32
    {
        public uint DwSize;

        public uint CntUsage;

        public uint Th32ProcessId;

        public IntPtr Th32DefaultHeapId;

        public uint Th32ModuleId;

        public uint CntThreads;

        public uint Th32ParentProcessId;

        public int PcPriClassBase;

        public uint DwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string SzExeFile;
    }
}
