using Content.Server.GameObjects.Components.Body;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Body
{
    [UsedImplicitly]
    public class BodySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(BodyManagerComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var bodyManager = entity.GetComponent<BodyManagerComponent>(); // TODO
            }
        }
    }
}
