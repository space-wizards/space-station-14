using System.Linq;
using Content.Client.Eui;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Starlight.GhostTheme;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Player;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.GhostTheme;

[UsedImplicitly]
public sealed class GhostThemeEui : BaseEui
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;

    private readonly GhostThemeWindow _window;

    public GhostThemeEui()
    {
        _window = new GhostThemeWindow(_preferencesManager);
        
        _window.SelectTheme += slot =>
        {
            base.SendMessage(new GhostThemeSelectedMessage(slot));
        };
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

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);
    }
}
