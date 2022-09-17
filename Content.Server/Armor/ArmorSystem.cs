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

            double coefDefaultPrice = 1; //default price of 1% protection against a certain type of damage
            var coefPriceList = new Dictionary<string, double>()
            {
                {"Brute", 3 }, //Group: Blunt, Slash, Piercing
                {"Blunt", 1 },
                {"Slash", 1 },
                {"Piercing", 1 },
                {"Burn", 3.6 }, //Group: Heat, Cold, Shock
                {"Heat", 1.2 },
                {"Cold", 1.2 },
                {"Shock", 1.2 },
                {"Toxin", 6.7 }, //Group: Radiation, Poison
                {"Radiation", 1.2 }, 
                {"Poison", 5 }, //  Armor stopping poison damage? Sounds strong
                {"Genetic", 2 }, //Group: Cellular
                {"Cellular", 2 }, //idk
                {"Airloss", 6 }, //Group: Bloodloss, Asphyxiation
                {"Bloodloss", 3 }, 
                {"Asphyxiation", 3 },
                {"Caustic", 6.2 } //Group: Heat, Poison
            };
            double flatDefaultPrice = 5; //default price of 1 damage protection against a certain type of damage
            var flatPriceList = new Dictionary<string, double>()
            {
                {"Brute", 15 }, //Group: Blunt, Slash, Piercing
                {"Blunt", 5 },
                {"Slash", 5 },
                {"Piercing", 5 },
                {"Burn", 30 }, //Group: Heat, Cold, Shock
                {"Heat", 10 },
                {"Cold", 10 },
                {"Shock", 10 },
                {"Toxin", 62 }, //Group: Radiation, Poison
                {"Radiation", 12 },
                {"Poison", 50 }, //  Armor stopping poison damage? Sounds strong
                {"Genetic", 20 }, //Group: Cellular
                {"Cellular", 20 }, //idk
                {"Airloss", 60 }, //Group: Bloodloss, Asphyxiation
                {"Bloodloss", 30 },
                {"Asphyxiation", 30 },
                {"Caustic", 60 } //Group: Heat, Poison
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
