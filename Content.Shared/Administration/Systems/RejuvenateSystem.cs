using Content.Shared.Rejuvenate;

namespace Content.Shared.Administration.Systems;

public sealed class RejuvenateSystem : EntitySystem
{
    /// <summary>
    /// Fully heals the target, removing all damage, debuffs or other negative status effects.
    /// </summary>
    public void PerformRejuvenate(EntityUid target)
    {
        RaiseLocalEvent(target, new RejuvenateEvent());
    }
}
