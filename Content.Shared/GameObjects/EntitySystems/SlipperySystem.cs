using Content.Shared.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SlipperySystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var slipperyComp in ComponentManager.EntityQuery<SharedSlipperyComponent>(true))
            {
                slipperyComp.Update();
            }
        }
    }
}
