using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.Events;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Atmos.Piping.EntitySystems;

/// <summary>
/// 
/// </summary>
public sealed partial class AtmosPipeNetSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
    private EntityQuery<AtmosPipeNetComponent> _netQuery = default!;
    private EntityQuery<AtmosPipeNodeComponent> _pipeQuery = default!;


    public override void Initialize()
    {
        base.Initialize();

        _netQuery = GetEntityQuery<AtmosPipeNetComponent>();
        _pipeQuery = GetEntityQuery<AtmosPipeNodeComponent>();

        SubscribeLocalEvent<AtmosPipeNetComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AtmosPipeNetComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<AtmosPipeNetComponent, NodeAddedEvent>(OnNodeAdded);
        SubscribeLocalEvent<AtmosPipeNetComponent, NodeRemovedEvent>(OnNodeRemoved);
        SubscribeLocalEvent<AtmosPipeNetComponent, SplitEvent>(OnSplit);
        SubscribeLocalEvent<AtmosPipeNetComponent, MergingEvent>(OnMerging);
    }

    public void Update(EntityUid uid, AtmosPipeNetComponent? pipeNet = null)
    {
        if (_netQuery.Resolve(uid, ref pipeNet))
            _atmosSystem.React(pipeNet.Air, pipeNet);
    }

    /// <summary>
    /// 
    /// </summary>
    public bool TryGetGas(Entity<AtmosPipeNodeComponent?, GraphNodeComponent?> pipe, [MaybeNullWhen(false)] out GasMixture gas)
    {
        gas = null;
        if (!Resolve(pipe, ref pipe.Comp1, ref pipe.Comp2))
            return false;

        if (!_netQuery.TryGetComponent(pipe.Comp2.GraphId, out var pipeNet))
            return false;

        gas = pipeNet.Air;
        return true;
    }


    /// <summary>
    /// 
    /// </summary>
    private void OnComponentStartup(EntityUid uid, AtmosPipeNetComponent comp, ComponentStartup args)
    {
        if (!TryComp<NodeGraphComponent>(uid, out var graph))
            return;
        if (graph.Nodes.FirstOrNull() is not { } nodeId)
            return;
        if (Transform(nodeId).GridUid is not { } gridId)
            return;

        comp.GridId = gridId;
        _atmosSystem.AddPipeNet(gridId, uid);
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnComponentShutdown(EntityUid uid, AtmosPipeNetComponent comp, ComponentShutdown args)
    {
        if (comp.GridId is not { } gridId)
            return;

        comp.GridId = null;
        _atmosSystem.RemovePipeNet(gridId, uid);
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnNodeAdded(EntityUid uid, AtmosPipeNetComponent comp, ref NodeAddedEvent args)
    {
        if (!_pipeQuery.TryGetComponent(args.Node, out var pipe))
            return;

        comp.Air.Volume += pipe.Volume;
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnNodeRemoved(EntityUid uid, AtmosPipeNetComponent comp, ref NodeRemovedEvent args)
    {
        if (!_pipeQuery.TryGetComponent(args.Node, out var pipe))
            return;

        // We want to do this whether or not the pipe is getting moved to a new graph
        // If it is we handle moving the gases in OnSplit/OnMerge.
        comp.Air.Multiply(comp.Air.Volume <= pipe.Volume ? 1f - pipe.Volume / comp.Air.Volume : 0f);
        comp.Air.Volume -= pipe.Volume;
    }

    /// <summary>
    /// Handles dividing gases between pipenets when a larger pipe network is split.
    /// </summary>
    private void OnSplit(EntityUid uid, AtmosPipeNetComponent comp, ref SplitEvent args)
    {
        if (!_netQuery.TryGetComponent(args.Split, out var split) || comp.Air.Volume + split.Air.Volume <= 0)
            return;

        _atmosSystem.DivideInto(comp.Air, new List<GasMixture>(2) { comp.Air, split.Air });
        comp.Air.Multiply(1f - comp.Air.Volume / (2 * comp.Air.Volume + split.Air.Volume)); // Since we are dividing into ourself we need to get rid of the excess (DivideInto doesn't remove gas from the source mix so this worst-case doubles our gas).
    }

    /// <summary>
    /// Handles combining gases in pipenets that are merged.
    /// </summary>
    private void OnMerging(EntityUid uid, AtmosPipeNetComponent comp, ref MergingEvent args)
    {
        if (!_netQuery.TryGetComponent(args.Merge, out var merging))
            return;

        _atmosSystem.Merge(comp.Air, merging.Air);
    }
}
