using Content.Shared.Damage;
using Content.Server.Examine;
using Content.Shared.Examine;
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
            SubscribeLocalEvent<ArmorComponent, ExamineGroupEvent>(OnExamineStats);
        }

        private void OnExamineStats(EntityUid uid, ArmorComponent component, ExamineGroupEvent args)
        {
            if (args.ExamineGroup != component.ExamineGroup)
                return;
            foreach (var coefficientArmor in component.Modifiers.Coefficients)
            {
                args.Entries.Add(new ExamineEntry(component.ExaminePriority,Loc.GetString("armor-coefficient-value",
                    ("type", coefficientArmor.Key),
                    ("value", MathF.Round((1f - coefficientArmor.Value) * 100, 1))
                    )));
            }

            foreach (var flatArmor in component.Modifiers.FlatReduction)
            {
                args.Entries.Add(new ExamineEntry(component.ExaminePriority + 0.1f,Loc.GetString("armor-reduction-value",
                    ("type", flatArmor.Key),
                    ("value", flatArmor.Value)
                    )));
            }
        }

        private void OnDamageModify(EntityUid uid, ArmorComponent component, DamageModifyEvent args)
        {
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, component.Modifiers);
        }

        private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (component.Modifiers == null)
                return;

            _examine.AddExamineGroupVerb(component.ExamineGroup, args);
        }
    }
}
