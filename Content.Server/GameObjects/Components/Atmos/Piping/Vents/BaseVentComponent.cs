using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Transfers gas from a <see cref="PipeNode"/> to the tile it is on.
    /// </summary>
    public abstract class BaseVentComponent : PipeNetDeviceComponent
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables]
        private PipeNode _ventInlet;

        private AtmosphereSystem _atmosSystem;

        public override void Initialize()
        {
            base.Initialize();
            _atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(BaseVentComponent)} on entity {Owner.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _ventInlet = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_ventInlet == null)
            {
                JoinedGridAtmos?.RemovePipeNetDevice(this);
                Logger.Error($"{typeof(BaseVentComponent)} on entity {Owner.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
        }

        public override void Update()
        {
            var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere(_entityManager);
            if (tileAtmos == null)
                return;
            VentGas(_ventInlet.Air, tileAtmos.Air);
            _atmosSystem.GetGridAtmosphere(Owner.Transform.GridID).Invalidate(tileAtmos.GridIndices);
        }

        protected abstract void VentGas(GasMixture inletGas, GasMixture outletGas);
    }
}
