using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Robust.Shared.Player;

namespace Content.Server.Objectives.Systems;

public sealed class WantonCarnageConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WantonCarnageConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, WantonCarnageConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        // Not escaping alive if you're deleted/dead
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        // You're not escaping if you're restrained!
        if (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0)
            return 0f;

        // There no emergency shuttles
        if (!_emergencyShuttle.EmergencyShuttleArrived)
            return 0f;

        // Check hijack for each emergency shuttle
        foreach (var stationData in EntityQuery<StationEmergencyShuttleComponent>())
        {
            if (stationData.EmergencyShuttle == null)
                continue;

            if (IsShuttleHijacked(stationData.EmergencyShuttle.Value, mindId))
                return 1f;
        }

        return 0f;
    }

    private bool IsShuttleHijacked(EntityUid shuttleGridId, EntityUid mindId)
    {
        var gridPlayers = Filter.BroadcastGrid(shuttleGridId).Recipients;
        var humanoids = GetEntityQuery<HumanoidAppearanceComponent>();
        EntityQuery<MobStateComponent>();
        var oneLeft = false;
        foreach (var player in gridPlayers)
        {
            if (player.AttachedEntity == null ||
                !_mind.TryGetMind(player.AttachedEntity.Value, out var crewMindId, out _))
                continue;

            if (mindId == crewMindId) // Let Space Asshole escape
            {
                continue;
            }

            var isHumanoid = humanoids.HasComponent(player.AttachedEntity.Value);
            if (!isHumanoid) // Only humanoids count as enemies
                continue;

            var isPersonIncapacitated = _mobState.IsDead(player.AttachedEntity.Value);
            if (isPersonIncapacitated) // Allow dead
                continue;

            if (oneLeft)
                return false;

            oneLeft = true;
        }

        return oneLeft;
    }
}
