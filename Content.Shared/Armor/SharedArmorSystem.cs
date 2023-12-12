using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
// stamina resistance begin
using Content.Shared.Damage.Events;
// stamina resistance end
namespace Content.Shared.Armor;

/// <summary>
/// This handles logic relating to <see cref="ArmorComponent"/>
/// </summary>
public abstract class SharedArmorSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<ArmorComponent, BorgModuleRelayedEvent<DamageModifyEvent>>(OnBorgDamageModify);
        SubscribeLocalEvent<ArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
        // stamina resistance begin
        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<StaminaModifyEvent>>(OnStaminaModify);
        // stamina resistance end
    }

    private void OnDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers);
    }

    private void OnBorgDamageModify(EntityUid uid, ArmorComponent component, ref BorgModuleRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers);
    }
    // stamina resistance begin
    private void OnStaminaModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<StaminaModifyEvent> args)
    {
        if (component.Modifiers.Coefficients.TryGetValue("Stamina", out var coefficient))
        {
            args.Args.Damage = args.Args.Damage * coefficient;
        }
    }
    // stamina resistance end

    private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = GetArmorExamine(component.Modifiers);

        var ev = new ArmorExamineEvent(examineMarkup);
        RaiseLocalEvent(uid, ref ev);

        _examine.AddDetailedExamineVerb(args, component, examineMarkup,
            Loc.GetString("armor-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("armor-examinable-verb-message"));
    }

    private FormattedMessage GetArmorExamine(DamageModifierSet armorModifiers)
    {
        var msg = new FormattedMessage();

        msg.AddMarkup(Loc.GetString("armor-examine"));

        foreach (var coefficientArmor in armorModifiers.Coefficients)
        {
            msg.PushNewline();
            // stamina resistance begin
            if (coefficientArmor.Key != "Stamina")
            {
                msg.AddMarkup(Loc.GetString("armor-coefficient-value",
                    ("type", coefficientArmor.Key),
                    ("value", MathF.Round((1f - coefficientArmor.Value) * 100,1))
                    ));
            }
            if (coefficientArmor.Key == "Stamina")
            {
                msg.AddMarkup(Loc.GetString("armor-coefficient-value-stamina",
                    ("type", coefficientArmor.Key),
                    ("value", MathF.Round((1f - coefficientArmor.Value) * 100,1))
                    ));
            }
            // stamina resistance end
        }

        foreach (var flatArmor in armorModifiers.FlatReduction)
        {
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("armor-reduction-value",
                ("type", flatArmor.Key),
                ("value", flatArmor.Value)
                ));
        }

        return msg;
    }
}
