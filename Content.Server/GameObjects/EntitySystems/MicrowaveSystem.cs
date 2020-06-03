using Content.Server.GameObjects.Components.Kitchen;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    public class MicrowaveSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(MicrowaveComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<MicrowaveComponent>();
                comp.OnUpdate();
            }
        }
    }
}
