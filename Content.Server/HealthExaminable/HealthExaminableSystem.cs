using Content.Server.Examine;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.HealthExaminable;

public sealed class HealthExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealthExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
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
            Message = Loc.GetString("health-examinable-verb-disabled"),
            IconTexture = "/Textures/Interface/VerbIcons/plus.svg.192dpi.png"
        };

        args.Verbs.Add(verb);
    }

    private FormattedMessage CreateMarkup(EntityUid uid, HealthExaminableComponent component, DamageableComponent damage)
    {
        var msg = new FormattedMessage();

        var first = true;
        foreach (var type in component.ExaminableTypes)
        {
            if (!damage.Damage.DamageDict.TryGetValue(type, out var dmg))
                continue;

            if (dmg == FixedPoint2.Zero)
                continue;

            FixedPoint2 closest = FixedPoint2.Zero;

            string chosenLocStr = string.Empty;
            foreach (var threshold in component.Thresholds)
            {
                var str = $"health-examinable-{component.LocPrefix}-{type}-{threshold}";
                var tempLocStr = Loc.GetString($"health-examinable-{component.LocPrefix}-{type}-{threshold}", ("target", uid));

                // i.e., this string doesn't exist, because theres nothing for that threshold
                if (tempLocStr == str)
                    continue;

                chosenLocStr = tempLocStr;

                if (dmg > threshold && threshold > closest)
                    closest = threshold;
            }

            if (closest == FixedPoint2.Zero)
                continue;

            if (!first)
            {
                msg.PushNewline();
            }
            else
            {
                first = false;
            }
            msg.AddMarkup(chosenLocStr);
        }

        if (msg.IsEmpty)
        {
            msg.AddMarkup(Loc.GetString($"health-examinable-{component.LocPrefix}-none"));
        }

        return msg;
    }
}
