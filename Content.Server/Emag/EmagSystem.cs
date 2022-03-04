using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;

namespace Content.Server.Emag
{
    public sealed class EmagSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var emag in EntityManager.EntityQuery<EmagComponent>())
            {
                if (emag.Charges == emag.MaxCharges)
                {
                    emag.Accumulator = 0;
                    continue;
                }

                emag.Accumulator += frameTime;

                if (emag.Accumulator < emag.RechargeTime)
                {
                    continue;
                }

                emag.Accumulator -= emag.RechargeTime;
                emag.Charges++;
            }
        }
    }
}
