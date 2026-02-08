using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.HealthExaminable;

public sealed class HealthExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealthExaminableComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HealthExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnComponentInit(Entity<HealthExaminableComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Thresholds.Sort();
    }

    private void OnGetExamineVerbs(EntityUid uid, HealthExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damage))
            return;

        var detailsRange = _examineSystem.IsInDetailsRange(args.User, uid);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateMarkup(uid, component, damage);
                _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("health-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("health-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public FormattedMessage CreateMarkup(EntityUid uid, HealthExaminableComponent component, DamageableComponent damage)
    {
        var msg = new FormattedMessage();

        var first = true;
        foreach (var type in component.ExaminableTypes)
        {
            if (!damage.Damage.DamageDict.TryGetValue(type, out var dmg))
                continue;

            if (dmg == FixedPoint2.Zero)
                continue;

            var chosenLocStr = string.Empty;
            for (var i = 0; i < component.Thresholds.Length; i++)
            {
                if (component.Thresholds[i] > dmg)
                    break;

                if (Loc.TryGetString($"health-examinable-{component.LocPrefix}-{type}-{i}", out var locStr, ("target", Identity.Entity(uid, EntityManager))))
                    chosenLocStr = locStr;
            }

            // No threshold string was found
            if (string.IsNullOrEmpty(chosenLocStr))
                continue;

            if (!first)
            {
                msg.PushNewline();
            }
            else
            {
                first = false;
            }
            msg.AddMarkupOrThrow(chosenLocStr);
        }

        if (msg.IsEmpty)
        {
            msg.AddMarkupOrThrow(Loc.GetString($"health-examinable-{component.LocPrefix}-none"));
        }

        // Anything else want to add on to this?
        RaiseLocalEvent(uid, new HealthBeingExaminedEvent(msg), true);

        return msg;
    }
}

/// <summary>
///     A class raised on an entity whose health is being examined
///     in order to add special text that is not handled by the
///     damage thresholds.
/// </summary>
public sealed class HealthBeingExaminedEvent
{
    public FormattedMessage Message;

    public HealthBeingExaminedEvent(FormattedMessage message)
    {
        Message = message;
    }
}
