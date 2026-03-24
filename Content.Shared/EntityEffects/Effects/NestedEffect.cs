using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Applies the effect of an <see cref="EntityEffectPrototype"/>.
/// </summary>
public sealed partial class NestedEffect : EntityEffectBase<NestedEffect>
{
    /// <summary>
    /// The effect prototype to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityEffectPrototype> Proto;

    private List<string> _conditions = new();
    private List<string> _effects = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var proto = prototype.Index(Proto);
        if (proto.GuidebookText is {} key)
            return Loc.GetString(key, ("chance", Probability));

        _effects.Clear();
        foreach (var effect in proto.Effects)
        {
            if (effect.EntityEffectGuidebookText(prototype, entSys) is not {} text)
                continue;

            // basically GuidebookReagentEffectDescription but independent of reagents and no linq
            _conditions.Clear();
            if (effect.Conditions is {} conditions)
            {
                foreach (var condition in conditions)
                {
                    _conditions.Add(condition.EntityConditionGuidebookText(prototype));
                }
            }

            var desc = Loc.GetString("guidebook-nested-effect-description",
                ("effect", text),
                ("chance", effect.Probability),
                ("conditionCount", _conditions.Count),
                ("conditions", ContentLocalizationManager.FormatList(_conditions)));
            _effects.Add(desc);
        }

        return _effects.Count == 0 ? null : string.Join("\n", _effects);
    }
}

/// <summary>
/// Handles <see cref="NestedEffect"/>.
/// </summary>
public sealed class NestedEffectSystem : EntityEffectSystem<TransformComponent, NestedEffect>
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<NestedEffect> args)
    {
        _effects.TryApplyEffect(ent, args.Effect.Proto, args.Scale, args.User);
    }
}
