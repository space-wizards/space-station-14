using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Construction.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public class GasPortableSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPortableComponent, AnchorAttemptEvent>(OnPortableAnchorAttempt);
            SubscribeLocalEvent<GasPortableComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        }

        private void OnPortableAnchorAttempt(EntityUid uid, GasPortableComponent component, AnchorAttemptEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform))
                return;

            // If we can't find any ports, cancel the anchoring.
            if(!FindGasPortIn(transform.GridID, transform.Coordinates, out _))
                args.Cancel();
        }

        private void OnAnchorChanged(EntityUid uid, GasPortableComponent portable, ref AnchorStateChangedEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(portable.PortName, out PipeNode? portableNode))
                return;

            portableNode.ConnectionsEnabled = args.Anchored;

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(GasPortableVisuals.ConnectedState, args.Anchored);
            }
        }

        private bool FindGasPortIn(GridId gridId, EntityCoordinates coordinates, [NotNullWhen(true)] out GasPortComponent? port)
        {
            port = null;

            if (!gridId.IsValid())
                return false;

            var grid = _mapManager.GetGrid(gridId);

            foreach (var entityUid in grid.GetLocal(coordinates))
            {
                if (EntityManager.TryGetComponent<GasPortComponent>(entityUid, out port))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
