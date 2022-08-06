using JetBrains.Annotations;

namespace Content.Server.Power.SMES
{
    [UsedImplicitly]
    internal sealed class PowerSmesSystem : EntitySystem
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
