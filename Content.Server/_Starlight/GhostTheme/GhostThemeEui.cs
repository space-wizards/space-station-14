using Content.Server.EUI;
using Content.Shared.Starlight.NewLife;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Shared.Starlight.GhostTheme;

public sealed class GhostThemeEui : BaseEui
{
    private readonly GhostThemeSystem _ghostThemeSystem;
    private readonly HashSet<int> _usedSlots;
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
