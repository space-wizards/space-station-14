using Content.Shared.Item;
using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;

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

    private void Deactivate(EntityUid uid, ItemToggleComponent comp, ref ItemToggleDeactivatedEvent args)
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
