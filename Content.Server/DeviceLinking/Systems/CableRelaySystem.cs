using Content.Server.DeviceLinking.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// Handles <see cref="CableRelayComponent"/>: toggles the powernet connection of the cables on the relay's tile
/// when it receives a signal. Severed cables stay in place but stop conducting.
/// </summary>
public sealed partial class CableRelaySystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private NodeGroupSystem _nodeGroup = default!;
    [Dependency] private SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CableRelayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CableRelayComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(Entity<CableRelayComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.TriggerPort);
    }

    private void OnSignalReceived(Entity<CableRelayComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.TriggerPort)
            return;

        // The relay only does anything while bolted down to a tile.
        var xform = Transform(ent);
        if (!xform.Anchored)
            return;

        ent.Comp.Severed = !ent.Comp.Severed;

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor((gridUid, grid), xform.Coordinates);

        foreach (var anchored in _map.GetAnchoredEntities((gridUid, grid), tile))
        {
            if (!TryComp<CableComponent>(anchored, out var cable))
                continue;

            if (ent.Comp.CableTypes != null && !ent.Comp.CableTypes.Contains(cable.CableType))
                continue;

            if (!TryComp<NodeContainerComponent>(anchored, out var nodeContainer) ||
                !_nodeContainer.TryGetNode<CableNode>(nodeContainer, "power", out var node))
                continue;

            // Disabling the node makes it stop connecting to the powernet, breaking it at this tile.
            node.Enabled = !ent.Comp.Severed;
            _nodeGroup.QueueReflood(node);
        }
    }
}
