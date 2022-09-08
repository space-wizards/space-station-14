using Content.Shared.Damage;
using Content.Server.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

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
