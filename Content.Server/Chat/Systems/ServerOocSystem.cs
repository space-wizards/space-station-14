using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Configuration;

namespace Content.Server.Chat.Systems;

/// <summary>
///     ServerOocSystem is responsible for handling enabling and disabling OOC when the round changes state.
/// </summary>
public sealed partial class ServerOocSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameChange);
    }

    private void OnGameChange(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.InRound:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, false);
                break;
            case GameRunLevel.PostRound:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, true);
                break;
            case GameRunLevel.PreRoundLobby:
            default:
                return;
        }
    }
}
