using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Content.Server.Plants.Components;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Plants
{
    public class PottedPlantSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PottedPlantComponent, ComponentStartup>(OnComponentSturtup);
        }

        private void OnComponentSturtup(EntityUid uid, PottedPlantComponent pottedPlant, ComponentStartup args)
        {
            if(EntityManager.TryGetComponent(uid, out SpriteComponent sprite))
            {
                sprite.DrawDepth = (int)DrawDepth.Overdoors;
            }
        }
    }
}
