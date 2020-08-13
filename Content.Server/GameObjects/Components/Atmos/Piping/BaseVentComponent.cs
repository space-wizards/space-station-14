using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Transfers gas from a <see cref="Pipe"/> to the tile it is on.
    /// </summary>
    public abstract class BaseVentComponent : Component
    {
        [ViewVariables]
        private PipeNode _ventInlet;

        public override void Initialize()
        {
            base.Initialize();
            _ventInlet = Owner.GetComponent<NodeContainerComponent>().Nodes.OfType<PipeNode>().First();
        }

        public void Update(float frameTime)
        {
            var tileAtmos = AtmosHelpers.GetTileAtmosphere(Owner.Transform.GridPosition);
            if (tileAtmos == null)
                return;
            VentGas(_ventInlet.Air, tileAtmos.Air, frameTime);
        }

        protected abstract void VentGas(GasMixture inletGas, GasMixture outletGas, float frameTime);
    }
}
