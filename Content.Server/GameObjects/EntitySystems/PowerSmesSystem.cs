using Content.Server.GameObjects.Components.Power;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class PowerSmesSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(SmesComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<SmesComponent>();
                comp.OnUpdate();
            }
        }
    }
}
