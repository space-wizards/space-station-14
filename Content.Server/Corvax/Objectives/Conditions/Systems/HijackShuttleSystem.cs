using Content.Server.Corvax.Objectives.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Corvax.Objectives.Systems;

public sealed class HijackShuttleSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HijackShuttleConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, HijackShuttleConditionComponent objcomp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity == null
            || !TryComp<TransformComponent>(args.Mind.OwnedEntity, out var xform))
        {
            args.Progress = 0f;
            return;
        }

        var shuttleHijacked = false;
        var agentIsAlive = !_mindSystem.IsCharacterDeadIc(args.Mind);
        var agentIsFree = !(EntityManager.TryGetComponent<CuffableComponent>(args.Mind.OwnedEntity, out var cuffed)
                                && cuffed.CuffedHandCount > 0); // You're not escaping if you're restrained!

        // Any emergency shuttle counts for this objective.
        var query = EntityManager.AllEntityQueryEnumerator<StationEmergencyShuttleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (IsShuttleHijacked(xform, comp.EmergencyShuttle))
            {
                shuttleHijacked = true;
                break;
            }
        }

        args.Progress = (shuttleHijacked && agentIsAlive && agentIsFree) ? 1f : 0f;
    }

    private bool IsShuttleHijacked(TransformComponent agentXform, EntityUid? shuttle)
    {
        if (shuttle == null)
            return false;

        var transformSys = EntityManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var lookupSys = EntityManager.EntitySysManager.GetEntitySystem<EntityLookupSystem>();
        var mindSystem = EntityManager.EntitySysManager.GetEntitySystem<MindSystem>();
        var roleSystem = EntityManager.EntitySysManager.GetEntitySystem<RoleSystem>();

        if (!EntityManager.TryGetComponent<MapGridComponent>(shuttle, out var shuttleGrid) ||
            !EntityManager.TryGetComponent<TransformComponent>(shuttle, out var shuttleXform))
        {
            return false;
        }

        var shuttleAabb = transformSys.GetWorldMatrix(shuttleXform).TransformBox(shuttleGrid.LocalAABB);
        var agentOnShuttle = shuttleAabb.Contains(transformSys.GetWorldPosition(agentXform));
        var entities = lookupSys.GetEntitiesIntersecting(shuttleXform.MapID, shuttleAabb);
        foreach (var entity in entities)
        {
            if (!_mindSystem.TryGetMind(entity, out var mindId, out var mind))
                continue;

            var isPersonTraitor = roleSystem.MindHasRole<TraitorRoleComponent>(mindId);
            if (isPersonTraitor)
                continue;

            var isPersonDead = mindSystem.IsCharacterDeadIc(mind);
            if (!isPersonDead)
                continue; // Fail if some crew alive

            var isPersonCuffed =
                EntityManager.TryGetComponent<CuffableComponent>(mindId, out var cuffed)
                && cuffed.CuffedHandCount == 0;
            if (isPersonCuffed)
                continue;

            return false;
        }
        // TODO: Allow pets?

        return agentOnShuttle;
    }
}
