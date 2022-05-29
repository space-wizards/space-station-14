using Content.Shared.Hands.Components;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Hands.EntitySystems;

// These functions are mostly unused except for some AI operator stuff
// Nothing stops them from being used in general. If they ever get used elsewhere, then this file probably needs to be renamed.

public abstract partial class SharedHandsSystem : EntitySystem
{
    public bool TrySelect(EntityUid uid, EntityUid? entity, SharedHandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (!IsHolding(uid, entity, out var hand, handsComp))
            return false;

        SetActiveHand(uid, hand, handsComp);
        return true;
    }

    public bool TrySelect<TComponent>(EntityUid uid, [NotNullWhen(true)] out TComponent? component, SharedHandsComponent? handsComp = null) where TComponent : Component
    {
        component = null;
        if (!Resolve(uid, ref handsComp, false))
            return false;

        foreach (var hand in handsComp.Hands.Values)
        {
            if (TryComp(hand.HeldEntity, out component))
                return true;
        }

        return false;
    }

    public bool TrySelectEmptyHand(EntityUid uid, SharedHandsComponent? handsComp = null) => TrySelect(uid, null, handsComp);
}
