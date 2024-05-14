using Content.Server.CombatMode.Disarm;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.Item;

public sealed class ItemToggleSystem : SharedItemToggleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleDisarmMalusComponent, ItemToggledEvent>(ToggleMalus);
    }

    private void ToggleMalus(Entity<ItemToggleDisarmMalusComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<DisarmMalusComponent>(ent, out var malus))
            return;

        if (args.Activated)
        {
            ent.Comp.DeactivatedDisarmMalus ??= malus.Malus;
            if (ent.Comp.ActivatedDisarmMalus is {} activatedMalus)
                malus.Malus = activatedMalus;
            return;
        }

        ent.Comp.ActivatedDisarmMalus ??= malus.Malus;
        if (ent.Comp.DeactivatedDisarmMalus is {} deactivatedMalus)
            malus.Malus = deactivatedMalus;
    }
}
