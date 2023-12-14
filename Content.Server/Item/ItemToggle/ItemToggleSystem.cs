using Content.Shared.Item;
using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;
using Content.Shared.Item.ItemToggle;

namespace Content.Server.Item;

public sealed class ItemToggleSystem : SharedItemToggleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, ItemToggleActivatedEvent>(Activate);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleDeactivatedEvent>(Deactivate);
    }

    private void Activate(EntityUid uid, ItemToggleComponent comp, ref ItemToggleActivatedEvent args)
    {
        if (TryComp<ItemToggleSharpComponent>(uid, out var itemSharpness))
        {
            if (itemSharpness.ActivatedSharp)
                EnsureComp<SharpComponent>(uid);
        }


        if (!TryComp<ItemToggleDisarmMalusComponent>(uid, out var itemToggleDisarmMalus) ||
            !TryComp<DisarmMalusComponent>(uid, out var malus))
            return;

        //Default the deactivated DisarmMalus to the item's value before activation happens.
        itemToggleDisarmMalus.DeactivatedDisarmMalus ??= malus.Malus;

        if (itemToggleDisarmMalus.ActivatedDisarmMalus != null)
        {
            malus.Malus = (float) itemToggleDisarmMalus.ActivatedDisarmMalus;
        }
    }

    private void Deactivate(EntityUid uid, ItemToggleComponent comp, ref ItemToggleDeactivatedEvent args)
    {
        if (TryComp<ItemToggleSharpComponent>(uid, out var itemSharpness))
        {
            if (!itemSharpness.ActivatedSharp)
                RemComp<SharpComponent>(uid);
        }

        if (!TryComp<ItemToggleDisarmMalusComponent>(uid, out var itemToggleDisarmMalus) ||
            !TryComp<DisarmMalusComponent>(uid, out var malus))
            return;

        if (itemToggleDisarmMalus.DeactivatedDisarmMalus != null)
        {
            malus.Malus = (float) itemToggleDisarmMalus.DeactivatedDisarmMalus;
        }
    }
}
