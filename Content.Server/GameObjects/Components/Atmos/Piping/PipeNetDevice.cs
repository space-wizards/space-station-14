using Content.Server.Atmos;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    public abstract class PipeNetDevice : Component
    {
        public abstract void Update();

        private IGridAtmosphereComponent CurrentGridAtmos => EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(Owner.Transform.GridID);

        private IGridAtmosphereComponent _joinedGridAtmos;

        public override void Initialize()
        {
            base.Initialize();
            _joinedGridAtmos = CurrentGridAtmos;
            _joinedGridAtmos
        }

        public override void OnRemove()
        {
            base.OnRemove();
        }
    }
}
