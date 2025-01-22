using Content.Server.EUI;
using Content.Shared.Starlight.GhostTheme;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI;

public sealed class GhostThemeEui : BaseEui
{
    private readonly GhostThemeSystem _ghostThemeSystem;
    public GhostThemeEui()
    {
        _ghostThemeSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GhostThemeSystem>();
    }

    public override GhostThemeEuiState GetNewState() => new();

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
    }

    public override void Closed()
    {
        base.Closed();

        _ghostThemeSystem.CloseEui(Player);
    }
}
