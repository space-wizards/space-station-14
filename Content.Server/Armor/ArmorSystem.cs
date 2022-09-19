using Content.Shared.Damage;
using Content.Server.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Server.Cargo.Systems;

namespace Content.Server.Armor
{
    public sealed class ArmorSystem : EntitySystem
    {

        [Dependency] private readonly ExamineSystem _examine = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ArmorComponent, DamageModifyEvent>(OnDamageModify);
            SubscribeLocalEvent<ArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
            SubscribeLocalEvent<ArmorComponent, PriceCalculationEvent>(GetArmorPrice);
        }

        private void GetArmorPrice(EntityUid uid, ArmorComponent component, ref PriceCalculationEvent args)
        {
            if (component.Modifiers == null)
                return;

            double price = 0;

            double coefDefaultPrice = 2; // default price of 1% protection against any type of damage
            var coefPriceList = new Dictionary<string, double>() // List of damage type and cost per 1% of protection
            {
                {"Brute", 6 }, //Group: Blunt, Slash, Piercing
                {"Blunt", 2 },
                {"Slash", 2 },
                {"Piercing", 2 },
                {"Burn", 7.5 }, //Group: Heat, Cold, Shock
                {"Heat", 2.5 },
                {"Cold", 2.5 },
                {"Shock", 2.5 },
                {"Toxin", 12.5 }, //Group: Radiation, Poison
                {"Radiation", 2.5 }, 
                {"Poison", 10 }, 
                {"Genetic", 5 }, //Group: Cellular
                {"Cellular", 5 }, 
                {"Airloss", 10 }, //Group: Bloodloss, Asphyxiation
                {"Bloodloss", 5 }, 
                {"Asphyxiation", 5 },
                {"Caustic", 12.5 } //Group: Heat, Poison
            };
            double flatDefaultPrice = 10; //default price of 1 damage protection against a certain type of damage
            var flatPriceList = new Dictionary<string, double>() // List of damage type and cost per 1 damage reduction
            {
                {"Brute", 30 }, //Group: Blunt, Slash, Piercing
                {"Blunt", 10 },
                {"Slash", 10 },
                {"Piercing", 10 },
                {"Burn", 60 }, //Group: Heat, Cold, Shock
                {"Heat", 20 },
                {"Cold", 20 },
                {"Shock", 20 },
                {"Toxin", 76 }, //Group: Radiation, Poison
                {"Radiation", 16 },
                {"Poison", 60 },
                {"Genetic", 30 }, //Group: Cellular
                {"Cellular", 30 },
                {"Airloss", 100 }, //Group: Bloodloss, Asphyxiation
                {"Bloodloss", 50 },
                {"Asphyxiation", 50 },
                {"Caustic", 80 } //Group: Heat, Poison
            };

            foreach (var modifier in component.Modifiers.Coefficients)
            {
                if (coefPriceList.ContainsKey(modifier.Key))
                {
                    price += coefPriceList[modifier.Key] * 100 * (1-modifier.Value);
                }
                else
                {
                    price += coefDefaultPrice * 100 * (1 - modifier.Value);
                }
            }
            foreach (var modifier in component.Modifiers.FlatReduction)
            {
                if (flatPriceList.ContainsKey(modifier.Key))
                {
                    price += flatPriceList[modifier.Key] * modifier.Value;
                }
                else
                {
                    price += flatDefaultPrice * modifier.Value;
                }
            }
            args.Price += price;
        }
        private void OnDamageModify(EntityUid uid, ArmorComponent component, DamageModifyEvent args)
        {
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, component.Modifiers);
        }

        private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            var armorModifiers = component.Modifiers;

            if (armorModifiers == null)
                return;

            var verb = new ExamineVerb()
            {
                Act = () =>
                {
                    var markup = GetArmorExamine(armorModifiers);
                    _examine.SendExamineTooltip(args.User, uid, markup, false, false);
                },
                Text = Loc.GetString("armor-examinable-verb-text"),
                Message = Loc.GetString("armor-examinable-verb-message"),
                Category = VerbCategory.Examine,
                IconTexture = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png"
            };

            args.Verbs.Add(verb);
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
