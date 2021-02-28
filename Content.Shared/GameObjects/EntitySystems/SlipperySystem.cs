#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

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
