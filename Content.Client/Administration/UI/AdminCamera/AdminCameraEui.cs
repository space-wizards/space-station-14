using System.Numerics;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.AdminCamera;

/// <summary>
/// Admin Eui for opening a viewport window to observe entities.
/// Use the "Open Camera" admin verb or the "camera" command to open.
/// </summary>
[UsedImplicitly]
public sealed partial class AdminCameraEui : BaseEui
{
    private readonly AdminCameraWindow _window;
    private readonly AdminCameraControl _control;

    // If not null the camera is in "popped out" mode and is in an external window.
    private OSWindow? _OSWindow;

    // The last location the window was located at in game.
    // Is used for getting knowing where to "pop in" external windows.
    private Vector2 _lastLocation;

    public AdminCameraEui()
    {
        _window = new AdminCameraWindow();
        _control = new AdminCameraControl();

        _window.Contents.AddChild(_control);

        _control.OnFollow += () => SendMessage(new AdminCameraFollowMessage());
        _window.OnClose += () =>
        {
            if (!_control.IsPoppedOut)
                SendMessage(new CloseEuiMessage());
        };

        _control.OnPopoutControl += () =>
        {
            if (_control.IsPoppedOut)
                PopIn();
            else
                PopOut();
        };
    }

    // Pop the window out into an external OS window
    private void PopOut()
    {
        _lastLocation = _window.Position;

        // TODO: When there is a way to have a minimum window size, enforce something!
        _OSWindow = new OSWindow
        {
            SetSize = _window.Size,
            Title = _window.Title ?? Loc.GetString("admin-camera-window-title-placeholder"),
        };

        _OSWindow.Show();

        if (_OSWindow.Root == null)
            return;

        _control.Orphan();
        _OSWindow.Root.AddChild(_control);

        _OSWindow.Closed += () =>
        {
            if (_control.IsPoppedOut)
                SendMessage(new CloseEuiMessage());
        };

        _control.IsPoppedOut = true;
        _control.PopControl.Text = Loc.GetString("admin-camera-window-pop-in");

        _window.Close();
    }

    // Pop the window back into the in game window.
    private void PopIn()
    {
        _control.Orphan();
        _window.Contents.AddChild(_control);

        _window.Open(_lastLocation);

        _control.IsPoppedOut = false;
        _control.PopControl.Text = Loc.GetString("admin-camera-window-pop-out");

        _OSWindow?.Close();
        _OSWindow = null;
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
