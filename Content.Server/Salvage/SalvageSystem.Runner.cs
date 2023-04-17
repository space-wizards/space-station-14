using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Salvage;
using Robust.Shared.Utility;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void InitializeRunner()
    {
        SubscribeLocalEvent<FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<FTLCompletedEvent>(OnFTLCompleted);
    }

    private void OnFTLCompleted(ref FTLCompletedEvent args)
    {
        if (!TryComp<SalvageExpeditionComponent>(args.MapUid, out var component))
            return;

        // Someone FTLd there so start announcement
        if (component.Stage != ExpeditionStage.Added)
            return;

        Report(args.Entity, "Supply", "salvage-expedition-announcement-added", ("duration", component.EndTime - _timing.CurTime));
        component.Stage = ExpeditionStage.Running;
    }

    private void OnFTLStarted(ref FTLStartedEvent ev)
    {
        if (!TryComp<SalvageExpeditionComponent>(ev.FromMapUid, out var expedition) ||
            !TryComp<SalvageExpeditionDataComponent>(expedition.Station, out var station))
        {
            return;
        }

        // Check if any shuttles remain.
        var query = EntityQueryEnumerator<ShuttleComponent, TransformComponent>();

        while (query.MoveNext(out _, out var xform))
        {
            if (xform.MapUid == ev.FromMapUid)
                return;
        }

        // Last shuttle has left so finish the mission.
        FinishExpedition(station, expedition);
    }

    // Runs the expedition
    private void UpdateRunner()
    {
        // Mining missions: NOOP

        // Structure missions
        var structureQuery = EntityQueryEnumerator<SalvageStructureExpeditionComponent, SalvageExpeditionComponent>();

        while (structureQuery.MoveNext(out var uid, out var structure, out var comp))
        {
            if (comp.Completed)
                continue;

            for (var i = 0; i < structure.Structures.Count; i++)
            {
                var objective = structure.Structures[i];

                if (Deleted(objective))
                {
                    structure.Structures.RemoveSwap(i);
                    // TODO: Announce.
                }
            }

            if (structure.Structures.Count == 0)
            {
                var mission = comp.MissionParams;
                comp.Completed = true;

                // TODO: Announce completion
            }
        }
    }
}
