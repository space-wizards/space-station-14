using Content.Server.GameObjects.Components.Kitchen;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class MicrowaveSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in ComponentManager.EntityQuery<MicrowaveComponent>(true))
            {
                comp.OnUpdate();
            }
        }
    }
}
