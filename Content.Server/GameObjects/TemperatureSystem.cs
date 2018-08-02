using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects
{
    class TemperatureSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(TemperatureComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<TemperatureComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
