using Content.Server.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    internal sealed class StunSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<StunnableComponent>())
            {
                component.Update(frameTime);
            }
        }
    }
}
