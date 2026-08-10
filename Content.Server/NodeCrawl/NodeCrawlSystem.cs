using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeCrawl;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.NodeContainer;
using Robust.Shared.Utility;

namespace Content.Server.NodeCrawl;

public sealed partial class NodeCrawlSystem : SharedNodeCrawlSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private BarotraumaSystem _barotrauma = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<NodeContainerComponent> _nodeContainerQuery;
    [Dependency] private EntityQuery<CrawlableNodeComponent> _crawlableQuery;
    [Dependency] private EntityQuery<BarotraumaComponent> _barotraumaQuery;
    [Dependency] private EntityQuery<NodeCrawlerMovementComponent> _movementQuery;
    [Dependency] private EntityQuery<InternalsComponent> _internalsQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NodeCrawlerComponent, InhaleLocationEvent>(OnInhaleLocation, after: [typeof(InternalsSystem)]);
    }

    [SubscribeLocalEvent]
    private void OnExhaleLocation(Entity<NodeCrawlerComponent> ent, ref ExhaleLocationEvent args)
    {
        if (GetAir(ent) is not { } air)
            return;

        args.Gas = air;
    }

    [SubscribeLocalEvent]
    private void OnGetAir(Entity<NodeCrawlerComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled || GetAir(ent) is not { } air)
            return;

        args.Gas = air;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnNodeGroupsRebuilt(Entity<CrawlableNodeComponent> ent, ref NodeGroupsRebuilt args)
    {
        if (!_nodeContainerQuery.TryGetComponent(ent, out var nodeContainer))
            return;

        ent.Comp.DeadEnd = false;
        var reachableNodes = ent.Comp.ReachableNodes;
        reachableNodes.Clear();
        foreach (var node in nodeContainer.Nodes.Values)
        {
            foreach (var reachable in node.ReachableNodes)
            {
                if (!CanReachNode(ent.Comp, reachable))
                    continue;

                DebugTools.Assert(_crawlableQuery.HasComponent(reachable.Owner),
                    $"Node {ToPrettyString(reachable.Owner)} reachable from {ToPrettyString(ent)} should be a crawlable node, but wasn't");

                if (!reachableNodes.Contains(reachable.Owner))
                    reachableNodes.Add(reachable.Owner);
            }

            if (node is PipeNode pipeNode &&
                node.ReachableNodes.Count < BitOperations.PopCount((uint)pipeNode.CurrentPipeDirection))
            {
                ent.Comp.DeadEnd = true;
            }
        }

        ent.Comp.DeadEnd |= reachableNodes.Count == 0;
        Dirty(ent);
    }

    private bool CanReachNode(CrawlableNodeComponent component, Node reachable)
    {
        if (component.ReachableNodeTypes.Count == 0)
            return true;

        foreach (var nodeType in component.ReachableNodeTypes)
        {
            if (nodeType == reachable.NodeGroupID)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a gas mixture contained within a node the <see cref="NodeCrawlerMovementComponent"/> entity is in.
    /// </summary>
    /// <returns>The gas mixture.</returns>
    private GasMixture? GetExistingAir(Entity<NodeCrawlerMovementComponent> movement)
    {
        if (movement.Comp.Node is not { } node)
            return null;

        if (!_nodeContainerQuery.TryGetComponent(node, out var nodeContainer))
            return null;

        foreach (var containedNode in nodeContainer.Nodes.Values)
        {
            if (containedNode is not PipeNode pipe)
                continue;

            return pipe.Air;
        }

        return null;
    }

    protected override void SetupAir(Entity<NodeCrawlerMovementComponent> movement)
    {
        base.SetupAir(movement);

        if (movement.Comp.HeldCrawler is not { } heldCrawler ||
            !_barotraumaQuery.TryGetComponent(heldCrawler, out var barotrauma))
        {
            return;
        }

        if (GetExistingAir(movement) is { } existingAir)
        {
            var pressure = existingAir.Pressure switch
            {
                // Adjust pressure based on equipment. Works differently depending on if it's "high" or "low".
                <= Atmospherics.WarningLowPressure => _barotrauma.GetFeltLowPressure(heldCrawler, barotrauma, existingAir.Pressure),
                >= Atmospherics.WarningHighPressure => _barotrauma.GetFeltHighPressure(heldCrawler, barotrauma, existingAir.Pressure),
                _ => existingAir.Pressure,
            };

            if (pressure is >= Atmospherics.HazardLowPressure and <= Atmospherics.HazardHighPressure)
                return;
        }

        var xform = Transform(movement);
        var indices = _transform.GetGridTilePositionOrDefault((movement, xform));

        if (_atmosphere.GetTileMixture(xform.GridUid, xform.MapUid, indices, true) is { Temperature: > 0f } environment)
        {
            // we want to get one atmosphere's worth of pressure in the air volume of the component
            // we need to take an amount of moles from the gas, so
            // PV = nRT
            // (Atmospherics.OneAtmosphere) * (movement.Comp.AirVolume) = (amount of mols) * R * (environment.Temperature)
            // solve for amount of mols
            // amount of mols = (Atmospherics.OneAtmosphere) * (movement.Comp.AirVolume) / R * (environment.Temperature)
            var transferMoles = Atmospherics.OneAtmosphere * movement.Comp.AirVolume / (environment.Temperature * Atmospherics.R);

            movement.Comp.Air = new(movement.Comp.AirVolume);
            _atmosphere.Merge(movement.Comp.Air, environment.Remove(transferMoles));
        }
    }

    private Entity<NodeCrawlerMovementComponent>? GetMovement(Entity<NodeCrawlerComponent> crawler)
    {
        if (!_movementQuery.TryGetComponent(crawler.Comp.Mover, out var mover))
            return null;

        return new(crawler.Comp.Mover.Value, mover);
    }

    protected override void EjectAir(Entity<NodeCrawlerMovementComponent> movement)
    {
        base.EjectAir(movement);

        if (movement.Comp.Air is not { } air)
            return;

        var xform = Transform(movement);
        var indices = _transform.GetGridTilePositionOrDefault((movement, xform));

        if (_atmosphere.GetTileMixture(xform.GridUid, xform.MapUid, indices, true) is not { } environment)
            return;

        _atmosphere.Merge(environment, air);
        air.Clear();
    }

    private Entity<NodeContainerComponent>? GetNodeContainer(Entity<NodeCrawlerComponent> crawler)
    {
        if (GetMovement(crawler) is not { } mover || mover.Comp.Node is not { } node)
            return null;

        if (!_nodeContainerQuery.TryGetComponent(node, out var nodeContainer))
            return null;

        return (node, nodeContainer);
    }

    /// <summary>
    /// Gets the air an entity with <see cref="NodeCrawlerComponent"/> is breathing.
    /// First it gets the air in the "safety bubble" in the <see cref="NodeCrawlerMovementComponent"/>
    /// Then the air contained in the node the entity is in.
    /// </summary>
    /// <returns>The found gas mixture.</returns>
    private GasMixture? GetAir(Entity<NodeCrawlerComponent> crawler)
    {
        if (GetMovement(crawler)?.Comp.Air is { } air)
            return air;

        if (GetNodeContainer(crawler) is not { } nodeContainer)
            return null;

        foreach (var containedNode in nodeContainer.Comp.Nodes.Values)
        {
            if (containedNode is not PipeNode pipe)
                continue;

            return pipe.Air;
        }

        return null;
    }

    private void OnInhaleLocation(Entity<NodeCrawlerComponent> ent, ref InhaleLocationEvent args)
    {
        if (GetAir(ent) is not { } air)
            return;

        if (_internalsQuery.TryGetComponent(ent, out var internals) && internals.GasTankEntity != null)
            return;

        args.Gas = air;
    }
}
