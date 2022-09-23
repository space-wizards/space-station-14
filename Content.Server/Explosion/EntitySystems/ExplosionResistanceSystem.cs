using Content.Server.Examine;
using Content.Server.Explosion.Components;
using Content.Shared.Examine;
using Content.Shared.Explosion;
using Content.Shared.Verbs;
using JetBrains.Annotations;


namespace Content.Server.Explosion.EntitySystems
{

    [UsedImplicitly]
    public sealed partial class ExplosionResistanceSystem : EntitySystem
    {

        [Dependency] private readonly ExamineSystem _examine = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ExplosionResistanceComponent, GetExplosionResistanceEvent>(OnGetResistance);
            SubscribeLocalEvent<ExplosionResistanceComponent, GetVerbsEvent<ExamineVerb>>(OnExamineVerb);
            SubscribeLocalEvent<ExplosionResistanceComponent, ExamineGroupEvent>(OnExamineStats);

        }

        private void OnExamineStats(EntityUid uid, ExplosionResistanceComponent component, ExamineGroupEvent args)
        {
            if (args.ExamineGroup != component.ExamineGroup)
                return;
            args.Entries.Add(new ExamineEntry(component.ExaminePriority, Loc.GetString("explosion-resistance-examine", ("value", MathF.Round((1f - component.DamageCoefficient) * 100)))));
        }

        private void OnExamineVerb(EntityUid uid, ExplosionResistanceComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            _examine.AddExamineGroupVerb(component.ExamineGroup, args);
        }

        private void OnGetResistance(EntityUid uid, ExplosionResistanceComponent component, GetExplosionResistanceEvent args)
        {
            args.DamageCoefficient *= component.DamageCoefficient;
            if (component.Resistances.TryGetValue(args.ExplotionPrototype, out var resistance))
                args.DamageCoefficient *= resistance;
        }

    }
}
