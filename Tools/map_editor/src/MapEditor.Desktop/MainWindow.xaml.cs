using System;
using System.Threading;
using System.Windows;
using MapEditor.RTBridge;
using Microsoft.Win32;

namespace MapEditor.Desktop;

public partial class MainWindow : Window
{
    private Thread? _rtThread;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
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

        // Wait for RT to finish initializing before enabling the menu.
        // The await continuation runs back on the WPF dispatcher thread
        // because WPF's sync context is captured on the await, so we can
        // safely touch StatusText here.
        try
        {
            await EditorContext.Ready;
            StatusText.Text = "RT ready. Use File > Open Map to load a YAML.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Waiting for RT init failed: {ex.Message}";
        }
    }

    private async void OnOpenMapClick(object sender, RoutedEventArgs e)
    {
        var context = EditorContext.Current;
        if (context == null)
        {
            StatusText.Text = "RT is still booting, try again in a moment.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Open SS14 Map",
            Filter = "SS14 Maps (*.yml)|*.yml|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != true)
            return;

        var path = dialog.FileName;
        StatusText.Text = $"Loading {System.IO.Path.GetFileName(path)}...";

        try
        {
            await context.LoadMapAsync(path);
            StatusText.Text = $"Loaded {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Load failed: {ex.Message}";
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();
}
