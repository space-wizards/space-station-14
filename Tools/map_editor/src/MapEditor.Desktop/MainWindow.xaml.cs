using System;
using System.Threading;
using System.Windows;
using MapEditor.RTBridge;

namespace MapEditor.Desktop;

public partial class MainWindow : Window
{
    private Thread? _rtThread;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Wait until the HwndHost has created its child HWND, then hand it
        // to RT on a dedicated STA worker thread. RT's main loop blocks and
        // needs STA on Windows, so we cannot use Task.Run (which is MTA).
        Dispatcher.InvokeAsync(() =>
        {
            var hwnd = RtViewport.ChildHwnd;
            if (hwnd == IntPtr.Zero)
            {
                StatusText.Text = "RtHwndHost did not produce a child HWND.";
                return;
            }

            StatusText.Text = $"Booting RT on STA thread with HWND 0x{hwnd.ToInt64():X}...";

            _rtThread = new Thread(() =>
            {
                try
                {
                    EditorBootstrap.Start(hwnd);
                }
                catch (Exception ex)
                {
                    // Surface the error on the UI thread.
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = $"RT boot failed: {ex.GetType().Name}\n{ex.Message}";
                    });
                }
            })
            {
                Name = "RT Game Thread",
                IsBackground = true,
            };
            _rtThread.SetApartmentState(ApartmentState.STA);
            _rtThread.Start();
        });
    }
}
