using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class SharedEntityInputSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<SharedEntityInputComponent>())
            {
                comp.Update();
            }
        }
    }
}
