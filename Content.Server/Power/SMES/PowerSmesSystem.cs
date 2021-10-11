using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.SMES
{
    [UsedImplicitly]
    internal class PowerSmesSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<SmesComponent>(true))
            {
                comp.OnUpdate();
            }
        }
    }
}
