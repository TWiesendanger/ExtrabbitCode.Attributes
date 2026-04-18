using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ExtrabbitCode.Inventor.Attributes.Helper;

public static partial class DockableWindowChildAdapter
{
    private const uint DlgcWantArrows = 0x0001;
    private const uint DlgcWantTab = 0x0002;
    private const uint DlgcWantAllKeys = 0x0004;
    private const uint DlgcHasSetSel = 0x0008;
    private const uint DlgcWantChars = 0x0080;
    private const int WmGetDlgCode = 0x0087;

    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExAppWindow = 0x00040000;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW",
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW",
        SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        nint dwNewLong);

    public static void AddWpfWindow(DockableWindow? dockableWindow, Window? window)
    {
        if (dockableWindow == null || window == null)
        {
            return;
        }

        PrepareWindow(window);

        WindowInteropHelper interopHelper = new(window)
        {
            Owner = new IntPtr(Globals.InvApp.MainFrameHWND)
        };

        if (!window.IsVisible)
        {
            window.Show();
        }

        IntPtr hwnd = interopHelper.EnsureHandle();

        RemoveAltTabPresence(hwnd);

        dockableWindow.AddChild(hwnd.ToInt64());

        HwndSource? hwndSource = HwndSource.FromHwnd(hwnd);
        hwndSource?.AddHook(WndProc);
    }

    private static void PrepareWindow(Window window)
    {
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Topmost = false;
    }

    private static void RemoveAltTabPresence(IntPtr hwnd)
    {
        nint exStylePtr = GetWindowLongPtr(hwnd, GwlExStyle);
        int exStyle = unchecked((int)exStylePtr);

        exStyle &= ~WsExAppWindow;
        exStyle |= WsExToolWindow;

        SetWindowLongPtr(hwnd, GwlExStyle, (nint)exStyle);
    }

    private static IntPtr WndProc(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (msg != WmGetDlgCode)
        {
            return IntPtr.Zero;
        }

        handled = true;

        return new IntPtr(
            DlgcWantChars
            | DlgcWantArrows
            | DlgcHasSetSel
            | DlgcWantTab
            | DlgcWantAllKeys);
    }
}