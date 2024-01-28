using Content.Shared.Item;
using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using ItemToggleComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleComponent;

namespace Content.Server.Item;

public sealed class ItemToggleSystem : SharedItemToggleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, ItemToggledEvent>(Toggle);
    }

    private void Toggle(EntityUid uid, ItemToggleComponent comp, ref ItemToggledEvent args)
    {
        if (args.Activated == true)
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
        else
        {
            if (TryComp<ItemToggleSharpComponent>(uid, out var itemSharpness))
            {
                if (itemSharpness.ActivatedSharp)
                    RemCompDeferred<SharpComponent>(uid);
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
}
