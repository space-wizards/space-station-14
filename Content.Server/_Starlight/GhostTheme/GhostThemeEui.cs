using Content.Server.EUI;
using Content.Shared.Starlight.GhostTheme;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Starlight;

namespace Content.Server.Ghost.Roles.UI;

public sealed class GhostThemeEui : BaseEui
{
    private readonly GhostThemeSystem _ghostThemeSystem;
    private readonly HashSet<string> _availableThemes;
    public GhostThemeEui(HashSet<string> availableThemes)
    {
        _ghostThemeSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GhostThemeSystem>();
        _availableThemes = availableThemes;
    }

    public override GhostThemeEuiState GetNewState() => new(){
        AvailableThemes = _availableThemes
    };

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        
        if (msg is not GhostThemeSelectedMessage selectedTheme)
        {
            Close();
            return;
        }
        
        _ghostThemeSystem.ChangeTheme(Player, selectedTheme.ID);
    }

    public override void Closed()
    {
        base.Closed();

        _ghostThemeSystem.CloseEui(Player);
    }
}
