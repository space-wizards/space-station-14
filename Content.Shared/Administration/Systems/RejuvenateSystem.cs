using Content.Shared.Rejuvenate;
using Content.Shared.Charges.Components;

namespace Content.Shared.Administration.Systems;

public sealed class RejuvenateSystem : EntitySystem
{
    /// <summary>
    /// Fully heals the target, removing all damage, debuffs or other negative status effects.
    /// </summary>
    public void PerformRejuvenate(EntityUid target)
    {
        RaiseLocalEvent(target, new RejuvenateEvent());

        if (!EntityManager.TransformQuery.TryGetComponent(target, out var xform))
            return;

        using var en = xform.ChildEnumerator;
        while (en.MoveNext(out var child))
        {
            if (EntityManager.HasComponent<LimitedChargesComponent>(child))
            {
                RaiseLocalEvent(child, new RejuvenateEvent());
            }
        }
    }
}
