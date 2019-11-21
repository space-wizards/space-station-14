using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Power.Chargers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class WeaponCapacitorChargerSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(WeaponCapacitorChargerComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<WeaponCapacitorChargerComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
