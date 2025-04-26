using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.AdminCamera;

/// <summary>
/// Admin Eui for opening a viewport window to observe entities.
/// Use the "Observe" admin verb to open.
/// </summary>
[UsedImplicitly]
public sealed partial class AdminCameraEui : BaseEui
{
    private readonly AdminCameraWindow _window;

    public AdminCameraEui()
    {
        _window = new AdminCameraWindow();
        _window.OnFollow += () => SendMessage(new AdminCameraFollowMessage());
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
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
