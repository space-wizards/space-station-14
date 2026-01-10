using Content.Shared.EntityConditions;
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

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var proto = prototype.Index(Proto);
        if (proto.GuidebookText is {} key)
            return Loc.GetString(key, ("chance", Probability));

        var effects = new List<string>();
        foreach (var effect in proto.Effects)
        {
            if (effect.EntityEffectGuidebookText(prototype, entSys) is {} text)
                effects.Add(text);
        }

        return effects.Count == 0 ? null : ContentLocalizationManager.FormatList(effects);
    }
}

/// <summary>
/// Handles <see cref="NestedEffect"/> and provides API for applying one directly in code.
/// </summary>
public sealed class NestedEffectSystem : EntityEffectSystem<TransformComponent, NestedEffect>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<NestedEffect> args)
    {
        ApplyNestedEffect(ent, args.Effect.Proto, args.Scale);
    }

    public void ApplyNestedEffect(EntityUid target, ProtoId<EntityEffectPrototype> id, float scale = 1f)
    {
        var proto = _proto.Index(id);
        if (_conditions.TryConditions(target, proto.Conditions))
            _effects.ApplyEffects(target, proto.Effects, scale);
    }
}
