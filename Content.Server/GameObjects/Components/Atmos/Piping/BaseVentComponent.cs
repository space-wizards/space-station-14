using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos
{
    public abstract class BaseVentComponent : Component
    {
        [ViewVariables]
        private PipeDirection _ventInletDirection;

        [ViewVariables]
        private Pipe _ventInlet;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _ventInletDirection, "ventInletDirection", PipeDirection.None);
        }

        public override void Initialize()
        {
            base.Initialize();
            var pipeContainer = Owner.GetComponent<PipeContainerComponent>();
            _ventInlet = pipeContainer.Pipes.Where(pipe => pipe.PipeDirection == _ventInletDirection).First();
        }

        public void Update(float frameTime)
        {
            var gridPosition = Owner.Transform.GridPosition;
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>()
                .GetGridAtmosphere(gridPosition.GridID);
            if (gridAtmos == null)
                return;
            var tile = gridAtmos.GetTile(gridPosition);
            if (tile == null)
                return;
            VentGas(_ventInlet.Air, tile.Air, frameTime);
        }

        protected abstract void VentGas(GasMixture inletGas, GasMixture outletGas, float frameTime);
    }
}
