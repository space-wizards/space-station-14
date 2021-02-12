using Content.Server.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ClimbSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<ClimbingComponent>(true))
            {
                comp.Update();
            }
        }
    }
}
