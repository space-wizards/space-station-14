using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Content.Shared.VotingNew;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.VotingNew;

public sealed class VoteCallNewEui : BaseEui
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public VoteCallNewEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        Dictionary<string, string> presets = new();
        Dictionary<string, string> presetsTypes = new();

        foreach (var preset in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>())
        {
            if(!preset.ShowInAdminVote)
                continue;

            if(_playerManager.PlayerCount < (preset.MinPlayers ?? int.MinValue))
                continue;

            if(_playerManager.PlayerCount > (preset.MaxPlayers ?? int.MaxValue))
                continue;

            presets[preset.ID] = Loc.GetString(preset.ModeTitle);
            presetsTypes[preset.ID] = preset.VoteType;
        }

        return new VoteCallNewEuiState(presets, presetsTypes);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case VoteCallNewEuiMsg.DoVote doVote:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
                {
                    Close();
                    break;
                }

                if (doVote.TargetPresetList.Count == 1)
                {
                    var preset = _prototypeManager.EnumeratePrototypes<GamePresetPrototype>()
                        .FirstOrDefault(x => x.ID == doVote.TargetPresetList[0]);

                    if (preset != null)
                    {
                        var ticker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();
                        ticker.SetGamePreset(preset.ID);
                    }
                }
                else
                {
                    var targetPresetList = string.Join(" ", doVote.TargetPresetList);
                    _consoleHost.RemoteExecuteCommand(Player, $"createvote Preset {targetPresetList}");
                }
                break;
        }
    }
}
