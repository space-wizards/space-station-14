using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Nodes.Components.Autolinkers;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.EntitySystems.Autolinkers;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasPortableSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;
        [Dependency] private readonly AtmosPipeNetSystem _pipeNodeSystem = default!;
        [Dependency] private readonly PortNodeSystem _portNodeSystem = default!;

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
            if (!_nodeSystem.TryGetNode<PortNodeComponent>(uid, portable.PortName, out var port))
                return;

            _portNodeSystem.SetConnectable((port.Owner, port.Comp2), args.Anchored);

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                _appearance.SetData(uid, GasPortableVisuals.ConnectedState, args.Anchored, appearance);
            }
        }

        public bool FindGasPortIn(EntityUid? gridId, EntityCoordinates coordinates, [NotNullWhen(true)] out GasPortComponent? port)
        {
            port = null;

            if (!_mapManager.TryGetGrid(gridId, out var grid))
                return false;

            foreach (var entityUid in grid.GetLocal(coordinates))
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
