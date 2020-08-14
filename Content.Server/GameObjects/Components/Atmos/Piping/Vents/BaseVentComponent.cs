using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Transfers gas from a <see cref="PipeNode"/> to the tile it is on.
    /// </summary>
    public abstract class BaseVentComponent : UpdatedPipingComponent
    {
        [ViewVariables]
        private PipeNode _ventInlet;

        private AtmosphereSystem _atmosSystem;

        public override void Initialize()
        {
            base.Initialize();
            _atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            _ventInlet = Owner.GetComponent<NodeContainerComponent>().Nodes.OfType<PipeNode>().First();
        }

        public override void Update(float frameTime)
        {
            var transform = Owner.Transform;
            var tileAtmos = AtmosHelpers.GetTileAtmosphere(transform.GridPosition);
            if (tileAtmos == null)
                return;
            VentGas(_ventInlet.Air, tileAtmos.Air, frameTime);
            _atmosSystem.GetGridAtmosphere(transform.GridID).Invalidate(tileAtmos.GridIndices);
        }

        protected abstract void VentGas(GasMixture inletGas, GasMixture outletGas, float frameTime);
    }
}
