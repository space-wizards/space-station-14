using Content.Server.GameObjects.Components.StatusEffects;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems.StatusEffects
{
    [UsedImplicitly]
    public sealed class StunSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(StunnableComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<StunnableComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
