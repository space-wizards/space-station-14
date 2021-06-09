using Content.Server.GameObjects.Components.Projectiles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ProjectileSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<ProjectileComponent>(true))
            {
                component.TimeLeft -= frameTime;

                if (component.TimeLeft <= 0)
                {
                    component.Owner.Delete();
                }
            }
        }
    }
}
