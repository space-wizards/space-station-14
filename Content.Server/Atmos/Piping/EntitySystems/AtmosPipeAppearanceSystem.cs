using Content.Server.Atmos.Piping.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.Events;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Piping.EntitySystems;

public sealed class AtmosPipeAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeAppearanceComponent, EdgeAddedEvent>(UpdateAppearanceOnRefEvent);
        SubscribeLocalEvent<PipeAppearanceComponent, EdgeRemovedEvent>(UpdateAppearanceOnRefEvent);
        SubscribeLocalEvent<PipeAppearanceComponent, ProxyNodeRelayEvent<EdgeAddedEvent>>(UpdateAppearanceOnRefEvent);
        SubscribeLocalEvent<PipeAppearanceComponent, ProxyNodeRelayEvent<EdgeRemovedEvent>>(UpdateAppearanceOnRefEvent);
    }

    private void UpdateAppearanceOnRefEvent<TEvent>(EntityUid uid, PipeAppearanceComponent component, ref TEvent args)
    {
        UpdateAppearance(uid);
    }

    private void UpdateAppearance(EntityUid uid, AppearanceComponent? appearance = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref appearance, ref xform, logMissing: false))
            return;

        if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
            return;

        // get connected entities
        var anyPipeNodes = false;
        HashSet<EntityUid> connected = new();
        foreach (var (nodeId, node) in _nodeSystem.EnumerateNodes(uid))
        {
            if (!HasComp<AtmosPipeNodeComponent>(nodeId))
                continue;

            anyPipeNodes = true;
            foreach (var (edgeId, _) in node.Edges)
            {
                if (HasComp<AtmosPipeNodeComponent>(edgeId))
                    connected.Add(_nodeSystem.GetNodeHost(edgeId));
            }
        }

        if (!anyPipeNodes)
            return;

        // find the cardinal directions of any connected entities
        var netConnectedDirections = PipeDirection.None;
        var tile = grid.TileIndicesFor(xform.Coordinates);
        foreach (var neighbour in connected)
        {
            var otherTile = grid.TileIndicesFor(Transform(neighbour).Coordinates);

            netConnectedDirections |= (otherTile - tile) switch
            {
                (0, 1) => PipeDirection.North,
                (0, -1) => PipeDirection.South,
                (1, 0) => PipeDirection.East,
                (-1, 0) => PipeDirection.West,
                _ => PipeDirection.None
            };
        }

        _appearance.SetData(uid, PipeVisuals.VisualState, netConnectedDirections, appearance);
    }
}
