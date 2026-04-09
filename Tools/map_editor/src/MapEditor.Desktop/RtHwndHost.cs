using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MapEditor.Desktop;

/// <summary>
///     WPF control that creates a blank Win32 child window and exposes its HWND
///     via <see cref="ChildHwnd"/>. Robust Toolbox is handed this HWND and wraps
///     it as its main window, so the RT viewport renders inside this control's
///     WPF layout slot.
/// </summary>
/// <remarks>
///     We register our own window class (instead of using the built in STATIC
///     class) with a <c>CS_OWNDC</c> style for the dedicated device context
///     SDL3 and OpenGL expect. The class <c>WndProc</c> is plain
///     <c>DefWindowProc</c>. We do not need to subclass the window proc
///     ourselves because SDL3 installs its own subclass when wrapping an
///     external HWND and routes mouse and keyboard events into RT's event
///     pipeline for free.
/// </remarks>
public sealed class RtHwndHost : HwndHost
{
    private const string ClassName = "MapEditorRtHost";
    private static bool _classRegistered;
    private static readonly object ClassLock = new();

    /// <summary>
    ///     The Win32 handle of the embedded child window. Zero until
    ///     <see cref="BuildWindowCore"/> has run.
    /// </summary>
    public IntPtr ChildHwnd { get; private set; }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        EnsureClassRegistered();

        const int WS_CHILD = 0x40000000;
        const int WS_VISIBLE = 0x10000000;
        const int WS_CLIPCHILDREN = 0x02000000;
        const int WS_CLIPSIBLINGS = 0x04000000;

        var childHwnd = CreateWindowEx(
            dwExStyle: 0,
            lpClassName: ClassName,
            lpWindowName: "RT Viewport",
            dwStyle: WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
            x: 0,
            y: 0,
            nWidth: 100,
            nHeight: 100,
            hWndParent: hwndParent.Handle,
            hMenu: IntPtr.Zero,
            hInstance: GetModuleHandle(null),
            lpParam: IntPtr.Zero);

        if (childHwnd == IntPtr.Zero)
        {
            var err = Marshal.GetLastPInvokeError();
            throw new InvalidOperationException(
                $"RtHwndHost: CreateWindowEx failed, Win32 error 0x{err:X}");
        }

        ChildHwnd = childHwnd;
        return new HandleRef(this, childHwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        // RT is configured with WindowOwned=false when wrapping this HWND so
        // it will not call SDL_DestroyWindow on its own. We own the window
        // on the WPF side and destroy it here.
        if (hwnd.Handle != IntPtr.Zero)
            DestroyWindow(hwnd.Handle);
        ChildHwnd = IntPtr.Zero;
    }

    private static void EnsureClassRegistered()
    {
        lock (ClassLock)
        {
            if (_classRegistered) return;

            const uint CS_HREDRAW = 0x0002;
            const uint CS_VREDRAW = 0x0001;
            const uint CS_OWNDC = 0x0020;

            var wndClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC,
                lpfnWndProc = GetDefWindowProcPtr(),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = GetModuleHandle(null),
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = ClassName,
                hIconSm = IntPtr.Zero,
            };

            var atom = RegisterClassExW(ref wndClass);
            if (atom == 0)
            {
                var err = Marshal.GetLastPInvokeError();
                // Class already registered in this process is fine, reuse it.
                if (err != 1410) // ERROR_CLASS_ALREADY_EXISTS
                {
                    throw new InvalidOperationException(
                        $"RtHwndHost: RegisterClassExW failed, Win32 error 0x{err:X}");
                }
            }

            _classRegistered = true;
        }
    }

    private static IntPtr GetDefWindowProcPtr()
    {
        // Bind to the unmanaged USER32!DefWindowProcW so we can use its
        // address as the class WndProc without having to keep a managed
        // delegate alive for the lifetime of the process.
        var user32 = GetModuleHandle("user32.dll");
        if (user32 == IntPtr.Zero)
            user32 = LoadLibraryW("user32.dll");
        return GetProcAddress(user32, "DefWindowProcW");
    }

    // ---- P/Invoke ----

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW wndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryW(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
}
