using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    public abstract class UpdatedPipingComponent : Component
    {
        public abstract void Update();

        public override void Initialize()
        {
            base.Initialize();
            //attatch to a GridAtmosphereComponent for updating
            var atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            var gridAtmos = atmosSystem.GetGridAtmosphere(Owner.Transform.GridID);
        }

        public override void OnRemove()
        {
            base.OnRemove();
        }
    }
}
