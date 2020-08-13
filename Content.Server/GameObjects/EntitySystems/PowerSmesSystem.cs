using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class PowerSmesSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in Robust.Shared.GameObjects.ComponentManager.EntityQuery<SmesComponent>())
            {
                comp.OnUpdate();
            }
        }
    }
}
