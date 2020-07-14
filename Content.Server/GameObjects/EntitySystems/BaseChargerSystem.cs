using Content.Server.GameObjects.Components.Power.Chargers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    internal class BaseChargerSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(BaseCharger));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<BaseCharger>().OnUpdate(frameTime);
            }
        }
    }
}
