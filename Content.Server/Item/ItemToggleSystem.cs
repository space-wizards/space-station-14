using Content.Shared.Item;
using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;

namespace Content.Server.Item.ItemToggleSystem;

public sealed class ItemToggleSystem : SharedItemToggleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, ItemToggleActivatedServerChangesEvent>(Activate);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleDeactivatedServerChangesEvent>(Deactivate);
    }

    private void Activate(EntityUid uid, ItemToggleComponent comp, ref ItemToggleActivatedServerChangesEvent args)
    {
        if (comp.ActivatedSharp)
            EnsureComp<SharpComponent>(uid);

        if (comp.ActivatedDisarmMalus != null)
        {
            if (TryComp<DisarmMalusComponent>(uid, out var malus))
            {
                malus.Malus = (float) comp.ActivatedDisarmMalus;
            }
        }
    }

    private void Deactivate(EntityUid uid, ItemToggleComponent comp, ref ItemToggleDeactivatedServerChangesEvent args)
    {
        if (!comp.ActivatedSharp)
            RemComp<SharpComponent>(uid);

        if (comp.DeactivatedDisarmMalus == null)
        {
            if (TryComp<DisarmMalusComponent>(uid, out var malus))
            {
                comp.DeactivatedDisarmMalus = malus.Malus;
            }
        }
        else
        {
            if (TryComp<DisarmMalusComponent>(uid, out var malus))
            {
                malus.Malus = (float) comp.DeactivatedDisarmMalus;
            }
        }
    }
}
