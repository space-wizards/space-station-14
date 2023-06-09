using Content.Server.Examine;
using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Blocking;

public sealed class BlockingSystem : SharedBlockingSystem
{
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockingComponent, GetVerbsEvent<ExamineVerb>>(OnVerbExamine);
    }

    private void OnVerbExamine(EntityUid uid, BlockingComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        _proto.TryIndex<DamageModifierSetPrototype>(component.PassiveBlockDamageModifer, out var passiveblockModifier);
        _proto.TryIndex<DamageModifierSetPrototype>(component.ActiveBlockDamageModifier, out var activeBlockModifier);

        var fraction = component.IsBlocking ? component.ActiveBlockFraction : component.PassiveBlockFraction;
        var modifier = component.IsBlocking ? activeBlockModifier : passiveblockModifier;

        var msg = new FormattedMessage();

        msg.AddMarkup(Loc.GetString("blocking-fraction", ("value", MathF.Round(fraction * 100, 1))));

        if (modifier != null)
        {
            AppendCoefficients(modifier, msg);
        }

        _examine.AddDetailedExamineVerb(args, component, msg,
            Loc.GetString("blocking-examinable-verb-text"),
            "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("blocking-examinable-verb-message")
        );
    }

    private static FormattedMessage AppendCoefficients(DamageModifierSet modifiers, FormattedMessage msg)
    {
        foreach (var coefficient in modifiers.Coefficients)
        {
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("blocking-coefficient-value",
                ("type", coefficient.Key),
                ("value", MathF.Round(coefficient.Value * 100, 1))
            ));
        }

        foreach (var flat in modifiers.FlatReduction)
        {
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("blocking-reduction-value",
                ("type", flat.Key),
                ("value", flat.Value)
            ));
        }

        return msg;
    }
}
