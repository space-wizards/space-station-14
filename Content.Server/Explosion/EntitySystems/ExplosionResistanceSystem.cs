using Content.Server.Examine;
using Content.Server.Explosion.Components;
using Content.Shared.Examine;
using Content.Shared.Explosion;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Utility;


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
            SubscribeLocalEvent<ExplosionResistanceComponent, ExamineStatsEvent>(OnExamineStats);

        }

        private void OnExamineStats(EntityUid uid, ExplosionResistanceComponent component, ExamineStatsEvent args)
        {
            args.Message.PushNewline();
            args.Message.AddMessage(ExamineExplosionResistance(component));
        }
        private FormattedMessage ExamineExplosionResistance(ExplosionResistanceComponent component)
        {
            var msg = new FormattedMessage();

            msg.AddMarkup(Loc.GetString("explosion-resistance-examine", ("value", MathF.Round((1f - component.DamageCoefficient) * 100))));

            return msg;
        }
        private void OnExamineVerb(EntityUid uid, ExplosionResistanceComponent component, GetVerbsEvent<ExamineVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (HasComp<CondensedExamineComponent>(uid))
                return;

            var verb = new ExamineVerb()
            {
                Act = () =>
                {
                    var markup = new FormattedMessage();
                    markup.AddMessage(ExamineExplosionResistance(component));
                    _examine.SendExamineTooltip(args.User, uid, markup, false, false);
                },
                Text = Loc.GetString("explosion-resistance-verb-text"),
                Message = Loc.GetString("explosion-resistance-verb-message"),
                Category = VerbCategory.Examine,
                IconTexture = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png"
            };

            args.Verbs.Add(verb);
        }
        private void OnGetResistance(EntityUid uid, ExplosionResistanceComponent component, GetExplosionResistanceEvent args)
        {
            args.DamageCoefficient *= component.DamageCoefficient;
            if (component.Resistances.TryGetValue(args.ExplotionPrototype, out var resistance))
                args.DamageCoefficient *= resistance;
        }

    }
}
