using Content.Server.Ghost;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.VentCraw.Components;
using Robust.Shared.Player;

namespace Content.Server.VentCraw;

public sealed class BeingVentCrawSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingVentCrawComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<BeingVentCrawComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<BeingVentCrawComponent, AtmosExposedGetAirEvent>(OnGetAir);
        SubscribeLocalEvent<BeingVentCrawComponent, MobStateChangedEvent>(OnMobStateChanged);
    }
    
    private void OnMobStateChanged(EntityUid uid, BeingVentCrawComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState != MobState.Critical)
            return;
            
        if (TryComp<ActorComponent>(uid, out var actor))
        {
            var session = actor.PlayerSession;
                
            var minds = _entities.System<SharedMindSystem>();
            if (!minds.TryGetMind(session, out var mindId, out var mind))
            {
                mindId = minds.CreateMind(session.UserId);
                mind = _entities.GetComponent<MindComponent>(mindId);
            }
                
            _entities.System<GhostSystem>().OnGhostAttempt(mindId, true, true, mind);
        }
    }

    private void OnGetAir(EntityUid uid, BeingVentCrawComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (!TryComp<VentCrawHolderComponent>(component.Holder, out var holder))
            return;

        if (holder.CurrentTube == null)
            return;

        if (!TryComp(holder.CurrentTube.Value, out NodeContainerComponent? nodeContainer))
            return;
        foreach (var nodeContainerNode in nodeContainer.Nodes)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, nodeContainerNode.Key, out PipeNode? pipe))
                continue;
            args.Gas = pipe.Air;
            args.Handled = true;
            return;
        }
    }

    private void OnInhaleLocation(EntityUid uid, BeingVentCrawComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<VentCrawHolderComponent>(component.Holder, out var holder))
            return;

        if (holder.CurrentTube == null)
            return;

        if (!TryComp(holder.CurrentTube.Value, out NodeContainerComponent? nodeContainer))
            return;
        foreach (var nodeContainerNode in nodeContainer.Nodes)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, nodeContainerNode.Key, out PipeNode? pipe))
                continue;
            args.Gas = pipe.Air;
            return;
        }
    }

    private void OnExhaleLocation(EntityUid uid, BeingVentCrawComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<VentCrawHolderComponent>(component.Holder, out var holder))
            return;

        if (holder.CurrentTube == null)
            return;

        if (!TryComp(holder.CurrentTube.Value, out NodeContainerComponent? nodeContainer))
            return;
        foreach (var nodeContainerNode in nodeContainer.Nodes)
        {
            if (!_nodeContainer.TryGetNode(nodeContainer, nodeContainerNode.Key, out PipeNode? pipe))
                continue;
            args.Gas = pipe.Air;
            return;
        }
    }
}
