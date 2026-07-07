using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Ghost.Roles;

/// <summary>
/// Client side of the ghost role party waiting dialog. Cancelling (button or
/// closing the window) tells the server to release the claimed party slot.
/// </summary>
[UsedImplicitly]
public sealed class GhostRolePartyWaitingEui : BaseEui
{
    private readonly GhostRolePartyWaitingWindow _window;

    public GhostRolePartyWaitingEui()
    {
        _window = new GhostRolePartyWaitingWindow();

        _window.CancelButton.OnPressed += _ =>
        {
            SendMessage(new GhostRolePartyCancelMessage());
            _window.Close();
        };

        // Closing the window any other way counts as cancelling too. If the
        // server already locked the party in, it ignores this message.
        _window.OnClose += () => SendMessage(new GhostRolePartyCancelMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is GhostRolePartyWaitingEuiState waiting)
            _window.SetCount(waiting.Ready, waiting.Total);
    }

    public override void Closed()
    {
        _window.Close();
    }
}

public sealed class GhostRolePartyWaitingWindow : DefaultWindow
{
    public readonly Button CancelButton;
    private readonly Label _label;

    public GhostRolePartyWaitingWindow()
    {
        Title = Loc.GetString("ghost-role-party-waiting-title");
        MinSize = new System.Numerics.Vector2(300, 120);

        _label = new Label
        {
            Text = Loc.GetString("ghost-role-party-waiting-label", ("ready", 0), ("total", 0)),
            HorizontalAlignment = HAlignment.Center,
        };

        CancelButton = new Button
        {
            Text = Loc.GetString("ghost-role-party-waiting-cancel"),
            HorizontalAlignment = HAlignment.Center,
        };

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            VerticalAlignment = VAlignment.Center,
            Children =
            {
                _label,
                CancelButton,
            },
        });
    }

    public void SetCount(int ready, int total)
    {
        _label.Text = Loc.GetString("ghost-role-party-waiting-label", ("ready", ready), ("total", total));
    }
}
