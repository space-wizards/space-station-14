using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class BallisticBulletSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(BallisticBulletComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                var component = entity.GetComponent<BallisticBulletComponent>();
                component.TimeLeft -= frameTime;

                if (component.TimeLeft <= 0)
                {
                    entity.Delete();
                }
            }
        }
    }
}
