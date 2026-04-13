using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MapEditor.RTBridge;
using Microsoft.Win32;

namespace MapEditor.Desktop;

public partial class MainWindow : Window
{
    private Thread? _rtThread;

    // Full list of spawnable prototypes, fetched once after RT init.
    private IReadOnlyList<SpawnablePrototype> _allPrototypes = Array.Empty<SpawnablePrototype>();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        EditorContext.Current?.Shutdown();
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

        // Wait for RT to finish initializing, then load the entity palette.
        try
        {
            var context = await EditorContext.Ready;
            StatusText.Text = "RT ready. Loading entity palette...";
            _allPrototypes = await context.GetSpawnablePrototypesAsync();
            StatusText.Text = $"RT ready. {_allPrototypes.Count} spawnable prototypes loaded.";
            ApplyEntityFilter(EntitySearchBox.Text);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Init failed: {ex.Message}";
        }
    }

    // ---- File menu ----

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

    // ---- Benchmark ----

    private async void OnRunBenchmarkClick(object sender, RoutedEventArgs e)
    {
        var context = EditorContext.Current;
        if (context == null)
        {
            StatusText.Text = "RT not ready.";
            return;
        }

        // Disable the button so we cannot stack runs on top of each other.
        RunBenchmarkButton.IsEnabled = false;
        BenchmarkResultText.Text = "Running...";
        StatusText.Text = "Benchmark: running...";
        try
        {
            var result = await context.RunBenchmarkAsync();
            BenchmarkResultText.Text = result.FormatReport();

            // Also write to a timestamped file in the user data folder so
            // multiple runs can be diffed.
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MapEditor", "Benchmarks");
                System.IO.Directory.CreateDirectory(dir);
                var file = System.IO.Path.Combine(dir, $"bench-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                System.IO.File.WriteAllText(file, result.FormatReport());
                StatusText.Text = $"Benchmark: done. {file}";
            }
            catch (Exception writeEx)
            {
                StatusText.Text = $"Benchmark: done (file write failed: {writeEx.Message})";
            }
        }
        catch (Exception ex)
        {
            BenchmarkResultText.Text = "";
            StatusText.Text = $"Benchmark failed: {ex.Message}";
        }
        finally
        {
            RunBenchmarkButton.IsEnabled = true;
        }
    }

    // ---- Entity palette ----

    private void OnEntitySearchChanged(object sender, TextChangedEventArgs e)
        => ApplyEntityFilter(EntitySearchBox.Text);

    private void ApplyEntityFilter(string? filter)
    {
        var needle = filter?.Trim() ?? "";
        EntityListBox.Items.Clear();

        // Cap the visible list so typing stays responsive even before the
        // user has filtered things down. The content bundle has thousands
        // of entities.
        const int maxVisible = 500;
        var count = 0;
        foreach (var proto in _allPrototypes)
        {
            if (needle.Length > 0)
            {
                if (proto.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0
                    && proto.Id.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
            }

            EntityListBox.Items.Add(new ListBoxItem
            {
                Content = string.IsNullOrWhiteSpace(proto.Name) ? proto.Id : $"{proto.Name}  [{proto.Id}]",
                Tag = proto,
            });

            count++;
            if (count >= maxVisible)
                break;
        }
    }

    private void OnEntitySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var context = EditorContext.Current;
        if (context == null)
            return;

        if (EntityListBox.SelectedItem is ListBoxItem item && item.Tag is SpawnablePrototype proto)
        {
            context.PlacementPrototypeId = proto.Id;
            SelectedEntityText.Text = $"> {proto.Name} [{proto.Id}]";
        }
        else
        {
            context.PlacementPrototypeId = null;
            SelectedEntityText.Text = "None selected";
        }
    }
}
