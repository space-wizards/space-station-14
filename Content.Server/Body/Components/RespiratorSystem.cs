using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Components
{
    [UsedImplicitly]
    public class RespiratorSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var respirator in EntityManager.EntityQuery<RespiratorComponent>(false))
            {
                respirator.Update(frameTime);
            }
        }
    }
}
