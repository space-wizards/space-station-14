using System.Linq;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.Administration.UI.AdminCamera;

/// <summary>
/// Admin Eui for opening a viewport window to observe entities.
/// Use the "Open Camera" admin verb or the "camera" command to open.
/// </summary>
[UsedImplicitly]
public sealed partial class AdminCameraEui : BaseEui
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private readonly AdminCameraWindow _window;

    public AdminCameraEui()
    {
        _window = new AdminCameraWindow();

        _window.OnFollow += () => SendMessage(new AdminCameraFollowMessage());
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnPopout += Popout;
    }

    private void Popout()
    {
        var monitor = _clyde.EnumerateMonitors().First();

        _window.Orphan();

        var clydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = _window.Title ?? Loc.GetString("admin-camera-window-title-placeholder"),
            Monitor = monitor,
            Width = 400,
            Height = 500,
        });

        var clydeRoot = _uiManager.CreateWindowRoot(clydeWindow);
        clydeRoot.AddChild(_window);

        clydeWindow.RequestClosed += _ => _window.Close();
        clydeWindow.DisposeOnClose = true;

        _window.PopoutButton.Disabled = true;
        _window.PopoutButton.Text = Loc.GetString("admin-camera-window-popped-out");
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase baseState)
    {
        if (baseState is not AdminCameraEuiState state)
            return;

        _window.SetState(state);
    }
}
