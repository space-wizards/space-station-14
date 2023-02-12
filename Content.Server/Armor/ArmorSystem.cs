using Content.Shared.Damage;
using Content.Server.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Server.Cargo.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;

namespace Content.Server.Armor
{
    public sealed class ArmorSystem : EntitySystem
    {
        const double CoefDefaultPrice = 2; // default price of 1% protection against any type of damage
        const double FlatDefaultPrice = 10; //default price of 1 damage protection against a certain type of damage

        [Dependency] private readonly ExamineSystem _examine = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
            SubscribeLocalEvent<ArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
            SubscribeLocalEvent<ArmorComponent, PriceCalculationEvent>(GetArmorPrice);
        }

        private void GetArmorPrice(EntityUid uid, ArmorComponent component, ref PriceCalculationEvent args)
        {
            if (component.Modifiers == null)
                return;

            double price = 0;

            foreach (var modifier in component.Modifiers.Coefficients)
            {
                _protoManager.TryIndex(modifier.Key, out DamageTypePrototype? damageType);

                if (damageType != null)
                {
                    price += damageType.ArmorPriceCoefficient * 100 * (1 - modifier.Value);
                }
                else
                {
                    price += CoefDefaultPrice * 100 * (1 - modifier.Value);
                }
            }
            foreach (var modifier in component.Modifiers.FlatReduction)
            {
                _protoManager.TryIndex(modifier.Key, out DamageTypePrototype? damageType);

                if (damageType != null)
                {
                    price += damageType.ArmorPriceFlat * modifier.Value;
                }
                else
                {
                    price += FlatDefaultPrice * modifier.Value;
                }
            }
            args.Price += price;
        }

        private void OnDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
        {
            args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers);
        }

        private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            var armorModifiers = component.Modifiers;

            if (armorModifiers == null)
                return;

            var examineMarkup = GetArmorExamine(armorModifiers);

            _examine.AddDetailedExamineVerb(args, component, examineMarkup, Loc.GetString("armor-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png", Loc.GetString("armor-examinable-verb-message"));
        }

        private static FormattedMessage GetArmorExamine(DamageModifierSet armorModifiers)
        {
            var msg = new FormattedMessage();

            msg.AddMarkup(Loc.GetString("armor-examine"));

            foreach (var coefficientArmor in armorModifiers.Coefficients)
            {
                msg.PushNewline();
                msg.AddMarkup(Loc.GetString("armor-coefficient-value",
                    ("type", coefficientArmor.Key),
                    ("value", MathF.Round((1f - coefficientArmor.Value) * 100,1))
                    ));
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
}
