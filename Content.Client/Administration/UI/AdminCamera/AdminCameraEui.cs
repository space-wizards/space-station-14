using System.Linq;
using System.Numerics;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

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
    private readonly AdminCameraControl _control;

    private WindowRoot? _windowRoot;

    private Vector2? _lastLocation;

    public AdminCameraEui()
    {
        _window = new AdminCameraWindow();
        _control = new AdminCameraControl();

        _control.OnFollow += () => SendMessage(new AdminCameraFollowMessage());
        _window.OnClose += () => SendMessage(new CloseEuiMessage());

        _window.Contents.AddChild(_control);
        _control.OnPopoutControl += () =>
        {
            if (_control.IsPoppedOut)
                PopIn();
            else
                PopOut();
        };
    }

    private void PopOut()
    {
        _lastLocation = _window.Position;

        var monitor = _clyde.EnumerateMonitors().First();

        var clydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = _window.Title ?? Loc.GetString("admin-camera-window-title-placeholder"),
            Monitor = monitor,
            Width = 400,
            Height = 500,
        });

        _control.Orphan();

        // TODO: When there is a way to have a minimum window size, enforce something!
        _windowRoot = _uiManager.CreateWindowRoot(clydeWindow);
        _windowRoot.AddChild(_control);

        clydeWindow.RequestClosed += _ =>
        {
            _window.Close();
        };
        clydeWindow.DisposeOnClose = true;

        // You can't close the window, otherwise the EUI will complain that your sending requests without it open.
        _window.Visible = false;

        _control.IsPoppedOut = true;
        _control.PopControl.Text = Loc.GetString("admin-camera-window-pop-in");
    }

    private void PopIn()
    {
        _control.Orphan();

        _window.Contents.AddChild(_control);
        if (_lastLocation != null)
            _window.Open(_lastLocation.Value);
        else
            _window.OpenCentered();

        _windowRoot?.Window.Dispose();
        _windowRoot = null;

        _control.IsPoppedOut = false;
        _control.PopControl.Text = Loc.GetString("admin-camera-window-pop-out");
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
        _control.SetState(state);
    }
}
