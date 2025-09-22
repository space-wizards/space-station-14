using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Database;
using Content.Shared.Localizations;

namespace Content.Shared.EntityEffects;

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SharedEntityEffectsSystem : EntitySystem, IEntityEffectRaiser
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _condition = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReactiveComponent, ReactionEntityEvent>(OnReactive);
    }

    private void OnReactive(Entity<ReactiveComponent> entity, ref ReactionEntityEvent args)
    {

        // TODO: Check if the effect should apply first!

        if (args.Reagent.ReactiveEffects != null && entity.Comp.ReactiveGroups != null)
        {
            foreach (var (key, val) in args.Reagent.ReactiveEffects)
            {
                if (!val.Methods.Contains(args.Method))
                    continue;

                if (!entity.Comp.ReactiveGroups.TryGetValue(key, out var group))
                    continue;

                if (!group.Contains(args.Method))
                    continue;

                foreach (var effect in val.Effects)
                {
                    effect.RaiseEvent(entity, this);
                }
            }
        }
    }

    public void ApplyEffects(EntityUid target, AnyEntityEffect[] effects)
    {
        // do all effects, if conditions apply
        foreach (var effect in effects)
        {
            ApplyEffect(target, effect);
        }
    }

    public void ApplyEffect(EntityUid target, AnyEntityEffect effect)
    {
        // See if conditions apply
        if (!_condition.TryConditions(target, effect.Conditions))
            return;

        // TODO: Logging
        /*
        if (effect.ShouldLog)
        {
            _adminLog.Add(
                LogType.ReagentEffect,
                effect.LogImpact,
                $"Metabolism effect {effect.GetType().Name:effect}"
                + $" of reagent {proto.LocalizedName:reagent}"
                + $" applied on entity {actualEntity:entity}"
                + $" at {Transform(actualEntity).Coordinates:coordinates}"
            );
        }*/

        effect.RaiseEvent(target, this);
    }

    public void RaiseEffectEvent<T>(EntityUid target, T effect) where T : EntityEffectBase<T>
    {
        var effectEv = new EntityEffectEvent<T>(effect);
        RaiseLocalEvent(target, ref effectEv);
    }
}

/// <summary>
/// This is a basic abstract entity effect containing all the data an entity effect needs to affect entities with effects...
/// </summary>
/// <typeparam name="T">The Component that is required for the effect</typeparam>
/// <typeparam name="TEffect">The Entity Effect itself</typeparam>
public abstract partial class EntityEffectSystem<T, TEffect> : EntitySystem where T : Component where TEffect : EntityEffectBase<TEffect>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<T, EntityEffectEvent<TEffect>>(Effect);
    }
    protected abstract void Effect(Entity<T> entity, ref EntityEffectEvent<TEffect> args);

    public string? GuidebookEffectDescription(TEffect effect)
    {
        if (effect.EntityEffectGuidebookText is null)
            return null;

        return Loc.GetString(
            effect.EntityEffectFormat,
            ("effect", effect),
            ("chance", effect.Probability),
            ("conditionCount", effect.Conditions?.Length ?? 0),
            ("conditions",
                ContentLocalizationManager.FormatList(
                    effect.Conditions?.Select(x => x.EntityConditionGuidebookText).ToList() ?? new List<string>()
                    )));
    }
}

public interface IEntityEffectRaiser
{
    void RaiseEffectEvent<T>(EntityUid target, T effect) where T : EntityEffectBase<T>;
}

[DataDefinition]
public abstract partial class EntityEffectBase<T> : AnyEntityEffect where T : EntityEffectBase<T>
{
    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser)
    {
        if (this is not T type)
            return;

        raiser.RaiseEffectEvent(target, type);
    }
}

// This exists so we can store entity effects in list and raise events without type erasure.
[DataDefinition]
public abstract partial class AnyEntityEffect
{
    public abstract void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser);

    [DataField]
    public AnyEntityCondition[]? Conditions;

    // TODO: This should be an entity condition
    [DataField]
    public float Probability = 1.0f;

    [DataField]
    public string EntityEffectFormat = "guidebook-reagent-effect-description";

    [DataField]
    public string? EntityEffectGuidebookText;

    public string? GuidebookEffectDescription()
    {
        if (EntityEffectGuidebookText is null)
            return null;

        return Loc.GetString(
            EntityEffectFormat,
            ("effect", this),
            ("chance", Probability),
            ("conditionCount", Conditions?.Length ?? 0),
            ("conditions",
                ContentLocalizationManager.FormatList(
                    Conditions?.Select(x => x.EntityConditionGuidebookText).ToList() ?? new List<string>()
                )));
    }

    [DataField]
    public virtual bool ShouldLog { get; private set; } = true;

    [DataField]
    public virtual LogImpact LogImpact { get; private set; } = LogImpact.Low;
}

/// <summary>
/// An Event carrying an entity effect.
/// </summary>
/// <param name="Effect">The Effect</param>
/// <param name="Scale">A strength scalar for the effect, defaults to 1 and typically only goes under for incomplete reactions.</param>
[ByRefEvent]
public readonly record struct EntityEffectEvent<T>(T Effect, float Scale = 1f) where T : EntityEffectBase<T>
{
    /// <summary>
    /// The Condition being raised in this event
    /// </summary>
    public readonly T Effect = Effect;

    /// <summary>
    /// The Scale modifier of this Effect.
    /// </summary>
    public readonly float Scale = Scale;
}
