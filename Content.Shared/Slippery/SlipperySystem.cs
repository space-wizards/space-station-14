using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Slippery
{
    [UsedImplicitly]
    public class SlipperySystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var slipperyComp in ComponentManager.EntityQuery<SlipperyComponent>().ToArray())
            {
                slipperyComp.Update();
            }
        }
    }
}
