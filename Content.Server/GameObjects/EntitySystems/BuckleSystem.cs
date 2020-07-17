using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems.Click;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class BuckleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(BuckleComponent));

            UpdatesAfter.Add(typeof(InteractionSystem));
            UpdatesAfter.Add(typeof(InputSystem));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (!entity.TryGetComponent(out BuckleComponent buckle))
                {
                    continue;
                }

                buckle.Update();
            }
        }
    }
}
