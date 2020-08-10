using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos
{
    public abstract class BaseScrubberComponent : Component
    {
        private PipeDirection _scrubberOutletDirection;

        private Pipe _scrubberOutlet;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _scrubberOutletDirection, "scrubberOutletDirection", PipeDirection.None);
        }

        public override void Initialize()
        {
            base.Initialize();
            var pipeContainer = Owner.GetComponent<PipeContainerComponent>();
            _scrubberOutlet = pipeContainer.Pipes.Where(pipe => pipe.PipeDirection == _scrubberOutletDirection).First();
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
            ScrubGas(_scrubberOutlet.Air, tile.Air, frameTime);
        }

        protected abstract void ScrubGas(GasMixture inletGas, GasMixture outletGas, float frameTime);
    }
}
