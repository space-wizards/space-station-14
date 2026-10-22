using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;

namespace Content.Shared.Hands.EntitySystems;

// These functions are mostly unused except for some AI operator stuff
// Nothing stops them from being used in general. If they ever get used elsewhere, then this file probably needs to be renamed.

public abstract partial class SharedHandsSystem
{
    public bool TrySelect(EntityUid uid, EntityUid? entity, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (!IsHolding((uid, handsComp), entity, out var hand))
            return false;

        SetActiveHand((uid, handsComp), hand);
        return true;
    }

    public bool TrySelect<TComponent>(EntityUid uid, [NotNullWhen(true)] out TComponent? component, HandsComponent? handsComp = null) where TComponent : Component
    {
        component = null;
        if (!Resolve(uid, ref handsComp, false))
            return false;

        foreach (var hand in handsComp.Hands.Keys)
        {
            if (!TryGetHeldItem((uid, handsComp), hand, out var held))
                continue;

            if (TryComp(held, out component))
                return true;
        }

        return false;
    }

    public bool TrySelectEmptyHand(EntityUid uid, HandsComponent? handsComp = null) => TrySelect(uid, null, handsComp);
}
