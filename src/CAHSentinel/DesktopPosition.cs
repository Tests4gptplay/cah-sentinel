using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CAHSentinel.Core;
using WinForms = System.Windows.Forms;

namespace CAHSentinel;

internal static class DesktopPosition
{
    // Use native desktop coordinates for both read/write and monitor work areas,
    // rather than mixing WPF device-independent units with screen pixels.
    public static void Restore(Window window, SentinelSettings settings)
    {
        if (settings.PetLeft is not int x || settings.PetTop is not int y) return;
        var handle = new WindowInteropHelper(window).Handle;
        var areas = WinForms.Screen.AllScreens.Select(s => new DesktopBounds(
            s.WorkingArea.X, s.WorkingArea.Y, s.WorkingArea.Width, s.WorkingArea.Height)).ToArray();
        if (areas.Length == 0) return;
        // Moving between monitors may change DPI and hence window pixel dimensions.
        for (var pass = 0; pass < 2; pass++)
        {
            if (!GetWindowRect(handle, out var rect)) return;
            var point = WindowPlacement.Restore(x, y, rect.Right - rect.Left, rect.Bottom - rect.Top, areas);
            if (!SetWindowPos(handle, IntPtr.Zero, point.X, point.Y, 0, 0, 0x0001 | 0x0004 | 0x0010)) return;
        }
    }

    public static bool Remember(Window window, SentinelSettings settings)
    {
        if (!GetWindowRect(new WindowInteropHelper(window).Handle, out var rect)) return false;
        settings.PetLeft = rect.Left;
        settings.PetTop = rect.Top;
        try { SettingsStore.Save(settings); return true; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);
}
