using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.Timing;
using Robust.Shared.Map.Components;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// Server side of <see cref="CableRelayComponent"/>: severs the cables on the relay's tile to match its switch,
/// handles signal toggling, and releases the cables when the relay is unanchored or destroyed.
/// </summary>
public sealed partial class CableRelaySystem : SharedCableRelaySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private NodeGroupSystem _nodeGroup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CableRelayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CableRelayComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CableRelayComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<CableRelayComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CableRelayComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnInit(Entity<CableRelayComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.TriggerPort);
        ApplyCables(ent);
    }

    private void OnShutdown(Entity<CableRelayComponent> ent, ref ComponentShutdown args)
    {
        ForEachCableOnTile(ent, static (_, node, sys) => sys.SetNodeEnabled(node, true));
    }

    private void OnSignalReceived(Entity<CableRelayComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.TriggerPort)
            return;

        // Cooldown against signal-spam refloods.
        if (TryComp<UseDelayComponent>(ent, out var useDelay) && !_useDelay.TryResetDelay((ent, useDelay), true))
            return;

        Toggle(ent);
    }

    private void OnPowerChanged(Entity<CableRelayComponent> ent, ref PowerChangedEvent args)
    {
        // Power must not touch the cables (only the switch + anchoring do); re-applying here would oscillate.
        UpdateUi(ent);
    }

    private void OnAnchorChanged(Entity<CableRelayComponent> ent, ref AnchorStateChangedEvent args)
    {
        ApplyCables(ent);
    }

    /// <summary>
    /// Re-derives every cable on the tile from the relay's switch and anchoring and refloods the powernet.
    /// Idempotent, and deliberately not gated on live power: the break latches to the switch so cutting the
    /// relay's own power line can't make it oscillate, and re-anchoring restores it without a manual flip.
    /// </summary>
    protected override void ApplyCables(Entity<CableRelayComponent> ent)
    {
        var active = ent.Comp.Severed && Transform(ent).Anchored;

        ForEachCableOnTile(ent, (cable, node, sys) =>
        {
            var shouldConnect = !(active && ent.Comp.AffectedTypes.Contains(cable.CableType));
            sys.SetNodeEnabled(node, shouldConnect);
        });
    }

    private void SetNodeEnabled(CableNode node, bool enabled)
    {
        if (node.Enabled == enabled)
            return;

        node.Enabled = enabled;
        _nodeGroup.QueueReflood(node);
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for every power cable anchored on the relay's tile.
    /// </summary>
    private void ForEachCableOnTile(Entity<CableRelayComponent> ent, Action<CableComponent, CableNode, CableRelaySystem> action)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor((gridUid, grid), xform.Coordinates);
        var cableQuery = GetEntityQuery<CableComponent>();
        var nodeQuery = GetEntityQuery<NodeContainerComponent>();

        foreach (var anchored in _map.GetAnchoredEntities((gridUid, grid), tile))
        {
            if (!cableQuery.TryGetComponent(anchored, out var cable))
                continue;

            if (!nodeQuery.TryGetComponent(anchored, out var nodeContainer) ||
                !_nodeContainer.TryGetNode<CableNode>(nodeContainer, "power", out var node))
                continue;

            action(cable, node, this);
        }
    }
}
