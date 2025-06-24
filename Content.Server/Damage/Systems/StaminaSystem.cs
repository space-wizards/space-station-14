using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Player;

namespace Content.Server.Damage.Systems;

public sealed partial class StaminaSystem : SharedStaminaSystem
{
    protected override void SetStaminaAnimation(Entity<StaminaComponent> entity)
    {
        if (!(entity.Comp.StaminaDamage > entity.Comp.AnimationThreshold))
            return;

        var filter = Filter.Pvs(entity);

        RaiseNetworkEvent(new StaminaAnimationEvent(GetNetEntity(entity)), filter);
    }
}
