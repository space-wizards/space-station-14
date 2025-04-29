using Content.Shared.Armor;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Inventory;

namespace Content.Shared.Damage.Systems;

public sealed partial class StaminaSystem
{
    private void InitializeResistance()
    {
        SubscribeLocalEvent<StaminaResistanceComponent, BeforeStaminaDamageEvent>(OnGetResistance);
        SubscribeLocalEvent<StaminaResistanceComponent, InventoryRelayedEvent<BeforeStaminaDamageEvent>>(RelayedResistance);
        SubscribeLocalEvent<StaminaResistanceComponent, ArmorExamineEvent>(OnArmorExamine);
    }

    private void OnGetResistance(Entity<StaminaResistanceComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Value *= ent.Comp.DamageCoefficient;
    }

    private void RelayedResistance(Entity<StaminaResistanceComponent> ent, ref InventoryRelayedEvent<BeforeStaminaDamageEvent> args)
    {
        if (ent.Comp.Worn)
            OnGetResistance(ent, ref args.Args);
    }

    private void OnArmorExamine(Entity<StaminaResistanceComponent> ent, ref ArmorExamineEvent args)
    {
        var value = MathF.Round((1f - ent.Comp.DamageCoefficient) * 100, 1);

        if (value == 0)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.Examine, ("value", value)));
    }
}
