using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class HasStatusEffect : IGraphCondition
{
    [DataField(required: true)]
    public EntProtoId Effect;

    [DataField]
    public bool ShouldHave = true;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        var statusEffects = entityManager.System<StatusEffectsSystem>();
        var hasStatusEffect = statusEffects.HasStatusEffect(uid, Effect);

        return hasStatusEffect == ShouldHave;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entity = args.Examined;
        var statusEffects = IoCManager.Resolve<IEntityManager>().System<StatusEffectsSystem>();
        var hasStatusEffect = statusEffects.HasStatusEffect(entity, Effect);

        switch (ShouldHave)
        {
            case true when !hasStatusEffect:
                args.PushMarkup(Loc.GetString("construction-examine-status-effect-should-have", ("effect", EffectName())));
                return true;
            case false when hasStatusEffect:
                args.PushMarkup(Loc.GetString("construction-examine-status-effect-should-not-have", ("effect", EffectName())));
                return true;
        }

        return false;
    }

    private string EffectName()
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (!protoMan.TryIndex(Effect, out var effectProtoData))
            return string.Empty;

        return effectProtoData.Name ?? string.Empty;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = ShouldHave
                ? "construction-step-condition-status-effect-should-have"
                : "construction-step-condition-status-effect-should-not-have",
            Arguments =
                [("effect", EffectName())],
        };
    }
}
