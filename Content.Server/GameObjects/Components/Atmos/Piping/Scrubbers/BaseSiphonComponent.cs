using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    /// <summary>
    ///     Transfers gas from the tile it is on to a <see cref="PipeNode"/>.
    /// </summary>
    public abstract class BaseSiphonComponent : UpdatedPipingComponent
    {
        [ViewVariables]
        private PipeNode _scrubberOutlet;

        private AtmosphereSystem _atmosSystem;

        public override void Initialize()
        {
            base.Initialize();
            _atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            _scrubberOutlet = Owner.GetComponent<NodeContainerComponent>().Nodes.OfType<PipeNode>().First();
        }

        public override void Update()
        {
            var tileAtmos = AtmosHelpers.GetTileAtmosphere(Owner.Transform.GridPosition);
            if (tileAtmos == null)
                return;
            ScrubGas(tileAtmos.Air, _scrubberOutlet.Air);
            _atmosSystem.GetGridAtmosphere(Owner.Transform.GridID).Invalidate(tileAtmos.GridIndices);
        }

        protected abstract void ScrubGas(GasMixture inletGas, GasMixture outletGas);
    }
}
