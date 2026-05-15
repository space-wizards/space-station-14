using Content.Shared.Silicons.StationAi;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Server.Player;
using Content.Shared.Popups;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiFixerConsoleSystem : SharedStationAiFixerConsoleSystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void FinalizeAction(Entity<StationAiFixerConsoleComponent> ent)
    {
        if (IsActionInProgress(ent) && ent.Comp.ActionTarget != null)
        {
            switch (ent.Comp.ActionType)
            {
                case StationAiFixerConsoleAction.Repair:

                    // Send message to disembodied player that they are being revived
                    if (_mind.TryGetMind(ent.Comp.ActionTarget.Value, out _, out var mind) &&
                        mind.IsVisitingEntity &&
                        _player.TryGetSessionById(mind.UserId, out var session))
                    {
                        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
                        _popup.PopupEntity(Loc.GetString("station-ai-fixer-console-repair-finished"), ent);
                    }
                    else
                    {
                        _popup.PopupEntity(Loc.GetString("station-ai-fixer-console-repair-successful"), ent);
                    }

                    // TODO: make predicted once a user is not required
                    if (ent.Comp.RepairFinishedSound != null)
                    {
                        _audio.PlayPvs(ent.Comp.RepairFinishedSound, ent);
                    }

                    break;

                case StationAiFixerConsoleAction.Purge:

                    _popup.PopupEntity(Loc.GetString("station-ai-fixer-console-purge-successful"), ent);

                    // TODO: make predicted once a user is not required
                    if (ent.Comp.PurgeFinishedSound != null)
                    {
                        _audio.PlayPvs(ent.Comp.PurgeFinishedSound, ent);
                    }

                    break;
            }
        }

        base.FinalizeAction(ent);
    }
}
