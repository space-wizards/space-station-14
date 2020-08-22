using Content.Server.GameObjects.Components.Projectiles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    internal sealed class ProjectileSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(ProjectileComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                var component = entity.GetComponent<ProjectileComponent>();
                component.TimeLeft -= frameTime;

                if (component.TimeLeft <= 0)
                {
                    entity.Delete();
                }
            }
        }
    }
}
