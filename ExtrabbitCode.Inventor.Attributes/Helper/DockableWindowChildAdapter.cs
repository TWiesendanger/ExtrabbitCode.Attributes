using System;
using System.Windows;
using System.Windows.Interop;

namespace ExtrabbitCode.Inventor.Attributes.Helper;

public static class DockableWindowChildAdapter
{
    private const uint DlgcWantArrows = 0x0001;
    private const uint DlgcWantTab = 0x0002;
    private const uint DlgcWantAllKeys = 0x0004;
    private const uint DlgcHasSetSel = 0x0008;
    private const uint DlgcWantChars = 0x0080;
    private const int WmGetDlgCode = 0x0087;

    public static void AddWpfWindow(DockableWindow? dockableWindow, Window? window)
    {
        if (window == null)
        {
            return;
        }

        window.WindowStyle = WindowStyle.None;
        window.WindowState = WindowState.Maximized;
        window.ResizeMode = ResizeMode.NoResize;
        window.ShowInTaskbar = false;
        window.Show();

        Window actualWindow = Window.GetWindow(window) ?? window;
        WindowInteropHelper interopHelper = new(actualWindow);
        IntPtr hwnd = interopHelper.EnsureHandle();

        dockableWindow?.AddChild(hwnd.ToInt64());

        HwndSource? hwndSource = HwndSource.FromHwnd(hwnd);
        hwndSource?.AddHook(WndProc);
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