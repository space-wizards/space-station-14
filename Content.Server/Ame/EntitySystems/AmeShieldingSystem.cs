using Content.Server.Ame.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.Events;
using Content.Shared.Ame;
using Robust.Server.GameObjects;

namespace Content.Server.Ame.EntitySystems;

public sealed class AmeShieldingSystem : EntitySystem
{
    [Dependency] private readonly AmeSystem _ameSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeShieldComponent, EdgeAddedEvent>(OnEdgeAdded);
        SubscribeLocalEvent<AmeShieldComponent, EdgeRemovedEvent>(OnEdgeRemoved);
        SubscribeLocalEvent<AmeShieldComponent, AddedToGraphEvent>(OnAddedToGraph);
        SubscribeLocalEvent<AmeShieldComponent, RemovedFromGraphEvent>(OnRemovedFromGraph);
        SubscribeLocalEvent<AmeShieldComponent, ProxyNodeRelayEvent<EdgeAddedEvent>>(OnEdgeAdded);
        SubscribeLocalEvent<AmeShieldComponent, ProxyNodeRelayEvent<EdgeRemovedEvent>>(OnEdgeRemoved);
        SubscribeLocalEvent<AmeShieldComponent, ProxyNodeRelayEvent<AddedToGraphEvent>>(OnAddedToGraph);
        SubscribeLocalEvent<AmeShieldComponent, ProxyNodeRelayEvent<RemovedFromGraphEvent>>(OnRemovedFromGraph);
    }


    /// <summary>Sets whether or not this particular segment of AME shielding is an AME core.</summary>
    private void SetCore(EntityUid shieldingId, bool value, AmeShieldComponent? shielding = null)
    {
        if (!Resolve(shieldingId, ref shielding))
            return;

        if (value == shielding.IsCore)
            return;

        shielding.IsCore = value;

        _appearanceSystem.SetData(shieldingId, AmeShieldVisuals.Core, value);

        foreach (var (graphId, _) in _nodeSystem.EnumerateGraphs(shieldingId))
        {
            if (!TryComp<AmeComponent>(graphId, out var ame))
                continue;

            if (value)
                _ameSystem.AddCore(graphId, shieldingId, ame);
            else
                _ameSystem.RemoveCore(graphId, shieldingId, ame);
        }
    }

    /// <summary>Updates the appearance of an AME core to reflect the current state of the AME.</summary>
    public void UpdateVisuals(EntityUid coreId, int injectionRatio, bool injecting, AmeShieldComponent? core = null)
    {
        if (!Resolve(coreId, ref core))
            return;

        if (!injecting)
        {
            _appearanceSystem.SetData(coreId, AmeShieldVisuals.CoreState, AmeCoreState.Off);
            _pointLightSystem.SetEnabled(coreId, false);
            return;
        }

        _pointLightSystem.SetRadius(coreId, Math.Clamp(injectionRatio, 1, 12));
        _pointLightSystem.SetEnabled(coreId, true);
        _appearanceSystem.SetData(coreId, AmeShieldVisuals.CoreState, injectionRatio > 2 ? AmeCoreState.Strong : AmeCoreState.Weak);
    }


    #region Event Handlers

    /// <summary>Makes AME shielding that is surrounded by AME shielding a core.</summary>
    private void OnEdgeAdded(EntityUid uid, AmeShieldComponent comp, ref EdgeAddedEvent args)
    {
        if (args.Node.Edges.Count >= 8 && !comp.IsCore)
            SetCore(uid, true, comp);
    }

    /// <summary>Makes AME shielding that is no longer surrounded by AME shielding no longer a core.</summary>
    private void OnEdgeRemoved(EntityUid uid, AmeShieldComponent comp, ref EdgeRemovedEvent args)
    {
        if (args.Node.Edges.Count < 8 && comp.IsCore)
            SetCore(uid, false, comp);
    }

    /// <summary>Adds AME cores that get added to AMEs to the AMEs list of cores.</summary>
    private void OnAddedToGraph(EntityUid uid, AmeShieldComponent comp, ref AddedToGraphEvent args)
    {
        if (!comp.IsCore || !TryComp<AmeComponent>(args.GraphId, out var ame))
            return;

        _ameSystem.AddCore(args.GraphId, uid, ame);
    }

    /// <summary>Removes AME cores that get removed from AMEs from the AMEs list of cores.</summary>
    private void OnRemovedFromGraph(EntityUid uid, AmeShieldComponent comp, ref RemovedFromGraphEvent args)
    {
        if (!comp.IsCore || !TryComp<AmeComponent>(args.GraphId, out var ame))
            return;

        _ameSystem.RemoveCore(args.GraphId, uid, ame);
    }

    private void OnEdgeAdded(EntityUid uid, AmeShieldComponent comp, ref ProxyNodeRelayEvent<EdgeAddedEvent> args)
        => OnEdgeAdded(uid, comp, ref args.Event);

    private void OnEdgeRemoved(EntityUid uid, AmeShieldComponent comp, ref ProxyNodeRelayEvent<EdgeRemovedEvent> args)
        => OnEdgeRemoved(uid, comp, ref args.Event);

    private void OnAddedToGraph(EntityUid uid, AmeShieldComponent comp, ref ProxyNodeRelayEvent<AddedToGraphEvent> args)
        => OnAddedToGraph(uid, comp, ref args.Event);

    private void OnRemovedFromGraph(EntityUid uid, AmeShieldComponent comp, ref ProxyNodeRelayEvent<RemovedFromGraphEvent> args)
        => OnRemovedFromGraph(uid, comp, ref args.Event);

    #endregion Event Handlers
}
