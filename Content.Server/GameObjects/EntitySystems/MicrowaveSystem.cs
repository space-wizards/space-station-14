using Content.Server.GameObjects.Components.Kitchen;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    internal sealed class MicrowaveSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in ComponentManager.EntityQuery<MicrowaveComponent>())
            {
                comp.OnUpdate();
            }
        }
    }
}
