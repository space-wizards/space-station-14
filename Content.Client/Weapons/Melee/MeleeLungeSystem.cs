using Content.Client.Weapons.Melee.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.Weapons.Melee
{
    [UsedImplicitly]
    public sealed class MeleeLungeSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var meleeLungeComponent in EntityManager.EntityQuery<MeleeLungeComponent>(true))
            {
                meleeLungeComponent.Update(frameTime);
            }
        }
    }
}
