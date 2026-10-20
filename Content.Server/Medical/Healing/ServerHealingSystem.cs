using Content.Shared.Medical;
using Content.Shared.Medical.Healing;
using Robust.Shared.Player;

namespace Content.Server.Medical.Healing;

/// <inheritdoc/>
public sealed partial class ServerHealingSystem : HealingSystem
{
    /// <inheritdoc />
    public override void PlayHealAnimation(EntityUid target, EntityUid? user = null)
    {
        var filter = Filter.Pvs(target).RemoveWhereAttachedEntity(e => e == user);
        RaiseNetworkEvent(new HealingAnimationEvent(GetNetEntity(target)), filter);
    }
}
