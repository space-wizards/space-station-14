// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.Leviathan.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Destructible;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles;

namespace Content.Server.DeadSpace.Necromorphs.Leviathan;

public sealed class LeviathanSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LeviathanComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<LeviathanComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<LeviathanComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<LeviathanComponent, DestructionEventArgs>(OnDestruction);
    }
    private void OnMindAdded(EntityUid uid, LeviathanComponent component, MindAddedMessage args)
    {
        TryTerminateGhost(component);

        var leviathan = Spawn(component.GhostLeviathanId, Transform(uid).Coordinates);

        if (TryComp<LeviathanGhostComponent>(leviathan, out var leviathanGhostComp))
            leviathanGhostComp.MasterEntity = uid;

        component.GhostLeviathanEntity = leviathan;

        if (!EntityManager.TryGetComponent<GhostRoleComponent>(leviathan, out var ghostRoleComponent))
        {
            _mindSystem.TransferTo(args.Mind, leviathan);
            component.MindFlag = true;
            return;
        }

        var id = ghostRoleComponent.Identifier;
        var session = args.Mind.Comp.Session;

        if (session != null)
        {
            EntityManager.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(session, id);
        }
        else
        {
            return;
        }

        component.MindFlag = true;
    }
    private void OnMindRemoved(EntityUid uid, LeviathanComponent component, MindRemovedMessage args)
    {
        if (component.MindFlag)
            TryTerminateGhost(component);
    }
    private void OnMobState(EntityUid uid, LeviathanComponent component, MobStateChangedEvent args)
    {
        if (_mobState.IsDead(uid))
            TryTerminateGhost(component);
    }
    private void OnDestruction(EntityUid uid, LeviathanComponent component, DestructionEventArgs args)
    {
        TryTerminateGhost(component);
    }
    private bool TryTerminateGhost(LeviathanComponent component)
    {
        if (component.GhostLeviathanEntity != null)
        {
            QueueDel(component.GhostLeviathanEntity);
            component.GhostLeviathanEntity = null;
            component.MindFlag = false;
            return true;
        }

        return false;
    }

}
