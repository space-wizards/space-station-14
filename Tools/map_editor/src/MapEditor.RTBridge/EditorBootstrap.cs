using System;
using Robust.Client;

namespace MapEditor.RTBridge;

/// <summary>
///     Entry point for booting Robust Toolbox inside the WPF host.
/// </summary>
/// <remarks>
///     The host provides an existing HWND (for example from a WPF
///     <see cref="System.Windows.Interop.HwndHost"/>) and we configure RT to
///     wrap it as its main window instead of creating a new OS window.
///     After this point the RT main loop blocks until it exits, so callers
///     should invoke <see cref="Start"/> on a dedicated worker thread.
/// </remarks>
public static class EditorBootstrap
{
    /// <summary>
    ///     Boots RT with the given HWND as its main window. Blocks for the
    ///     lifetime of the RT game loop, so callers should invoke it on a
    ///     background STA thread.
    /// </summary>
    public static void Start(IntPtr externalHwnd)
    {
        if (externalHwnd == IntPtr.Zero)
            throw new ArgumentException("externalHwnd must not be zero", nameof(externalHwnd));

        var options = new GameControllerOptions
        {
            // Keep the editor's user data isolated from the real game.
            UserDataDirectoryName = "MapEditor",
            // Wrap the host's HWND instead of creating a new top level window.
            // This is the flag the host side RT changes add to
            // GameControllerOptions so we can embed.
            MainWindowExternalHwnd = externalHwnd,
        };

        // ContentStart.StartLibrary is the supported entry point for
        // embedding RT inside another .NET process. It blocks until the game
        // loop exits.
        ContentStart.StartLibrary(Array.Empty<string>(), options);
    }
}
