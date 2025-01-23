using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasPortableSystem : EntitySystem
    {
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPortableComponent, AnchorAttemptEvent>(OnPortableAnchorAttempt);
            // Shouldn't need re-anchored event.
            SubscribeLocalEvent<GasPortableComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        }

        private void OnPortableAnchorAttempt(EntityUid uid, GasPortableComponent component, AnchorAttemptEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform))
                return;

            // If we can't find any ports, cancel the anchoring.
            if (!FindGasPortIn(transform.GridUid, transform.Coordinates, out _))
                args.Cancel();
        }

        private void OnAnchorChanged(EntityUid uid, GasPortableComponent portable, ref AnchorStateChangedEvent args)
        {
            if (!_nodeContainer.TryGetNode(uid, portable.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = args.Anchored;
        }

        public bool FindGasPortIn(EntityUid? gridId, EntityCoordinates coordinates, [NotNullWhen(true)] out GasPortComponent? port)
        {
            port = null;

            if (!TryComp<MapGridComponent>(gridId, out var grid))
                return false;

            foreach (var entityUid in _mapSystem.GetLocal(gridId.Value, grid, coordinates))
            {
                if (EntityManager.TryGetComponent(entityUid, out port))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
