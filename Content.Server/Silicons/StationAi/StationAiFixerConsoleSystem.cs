using Content.Shared.Silicons.StationAi;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Content.Shared.Ghost;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiFixerConsoleSystem : SharedStationAiFixerConsoleSystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    protected override void OnInserted(Entity<StationAiFixerConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        base.OnInserted(ent, ref args);

        if (!TryGetTarget(ent, out var target))
            return;

        if (_mind.TryGetMind(target.Value, out var mindId, out var mind))
        {
            _ghost.OnGhostAttempt(mindId, canReturnGlobal: true, mind: mind);

            // Don't allow the player to manually return to their body
            // so they don't get stuck in it
            if (TryComp<GhostComponent>(mind.VisitingEntity, out var ghost))
            {
                _ghost.SetCanReturnToBody((mind.VisitingEntity.Value, ghost), false);
            }
        }
    }

    protected override void FinalizeAction(Entity<StationAiFixerConsoleComponent> ent)
    {
        if (IsActionInProgress(ent) && ent.Comp.ActionTarget != null)
        {
            if (ent.Comp.ActionType == StationAiFixerConsoleAction.Repair)
            {
                // Send message to disembodied player that they are being revived
                if (_mind.TryGetMind(ent.Comp.ActionTarget.Value, out _, out var mind) &&
                    mind.IsVisitingEntity &&
                    _player.TryGetSessionById(mind.UserId, out var session))
                {
                    _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
                }

                // TODO: make predicted once a user is not required
                if (ent.Comp.RepairFinishedSound != null)
                {
                    _audio.PlayPvs(ent.Comp.RepairFinishedSound, ent);
                }
            }
            else if (ent.Comp.ActionType == StationAiFixerConsoleAction.Purge)
            {
                // TODO: make predicted once a user is not required
                if (ent.Comp.PurgeFinishedSound != null)
                {
                    _audio.PlayPvs(ent.Comp.PurgeFinishedSound, ent);
                }
            }
        }

        base.FinalizeAction(ent);
    }
}
