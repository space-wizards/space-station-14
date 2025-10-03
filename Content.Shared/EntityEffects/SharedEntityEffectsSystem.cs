using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Database;
using Content.Shared.EntityConditions;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

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
        if (args.Reagent.ReactiveEffects == null || entity.Comp.ReactiveGroups == null)
            return;

        var scale = args.ReagentQuantity.Quantity.Float();

        foreach (var (key, val) in args.Reagent.ReactiveEffects)
        {
            if (!val.Methods.Contains(args.Method))
                continue;

            if (!entity.Comp.ReactiveGroups.TryGetValue(key, out var group))
                continue;

            if (!group.Contains(args.Method))
                continue;

            ApplyEffects(entity, val.Effects, scale);
        }

        if (entity.Comp.Reactions == null)
            return;

        foreach (var entry in entity.Comp.Reactions)
        {
            if (!entry.Methods.Contains(args.Method))
                continue;

            if (entry.Reagents == null || entry.Reagents.Contains(args.Reagent.ID))
                ApplyEffects(entity, entry.Effects, scale);
        }
    }

    /// <summary>
    /// Applies a list of entity effects to a target entity.
    /// </summary>
    /// <param name="target">Entity being targeted by the effects</param>
    /// <param name="effects">Effects we're applying to the entity</param>
    /// <param name="scale">Optional scale multiplier for the effects</param>
    public void ApplyEffects(EntityUid target, EntityEffect[] effects, float scale = 1f)
    {
        // do all effects, if conditions apply
        foreach (var effect in effects)
        {
            TryApplyEffect(target, effect, scale);
        }
    }

    /// <summary>
    /// Applies an entity effect to a target if all conditions pass.
    /// </summary>
    /// <param name="target">Target we're applying an effect to</param>
    /// <param name="effect">Effect we're applying</param>
    /// <param name="scale">Optional scale multiplier for the effect. Not all </param>
    /// <returns>True if all conditions pass!</returns>
    public bool TryApplyEffect(EntityUid target, EntityEffect effect, float scale = 1f)
    {
        if (scale < effect.MinScale)
            return false;

        // See if conditions apply
        if (!_condition.TryConditions(target, effect.Conditions))
            return false;

        ApplyEffect(target, effect, scale);
        return true;
    }

    /// <summary>
    /// Applies an <see cref="EntityEffect"/> to a given target.
    /// This doesn't check conditions so you should only call this if you know what you're doing!
    /// </summary>
    /// <param name="target">Target we're applying an effect to</param>
    /// <param name="effect">Effect we're applying</param>
    /// <param name="scale">Optional scale multiplier for the effect. Not all </param>
    public void ApplyEffect(EntityUid target, EntityEffect effect, float scale = 1f)
    {
        // Clamp the scale if the effect doesn't allow scaling.
        if (effect.Scaling)
            scale = Math.Min(scale, 1f);

        if (effect.ShouldLog)
        {
            _adminLog.Add(
                LogType.EntityEffect,
                effect.LogImpact,
                $"Entity effect {effect.GetType().Name:effect}"
                + $" applied on entity {target:entity}"
                + $" at {Transform(target).Coordinates:coordinates}"
                + $" with a scale multiplier of {scale}"
            );
        }

        effect.RaiseEvent(target, this, scale);
    }

    /// <summary>
    /// Raises an effect to an entity. You should not be calling this unless you know what you're doing.
    /// </summary>
    public void RaiseEffectEvent<T>(EntityUid target, T effect, float scale) where T : EntityEffectBase<T>
    {
        var effectEv = new EntityEffectEvent<T>(effect, scale);
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
        // TODO: Temporary relay until Metabolism can properly separate effects/conditions for organs and bodies
        SubscribeLocalEvent<OrganComponent, EntityEffectEvent<TEffect>>(RelayEffect);
    }

    protected abstract void Effect(Entity<T> entity, ref EntityEffectEvent<TEffect> args);

    private void RelayEffect(Entity<OrganComponent> entity, ref EntityEffectEvent<TEffect> args)
    {
        if (entity.Comp.Body is not { } body)
            return;

        RaiseLocalEvent(body, ref args);
    }
}

public interface IEntityEffectRaiser
{
    void RaiseEffectEvent<T>(EntityUid target, T effect, float scale) where T : EntityEffectBase<T>;
}

public abstract partial class EntityEffectBase<T> : EntityEffect where T : EntityEffectBase<T>
{
    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale)
    {
        if (this is not T type)
            return;

        raiser.RaiseEffectEvent(target, type, scale);
    }
}

// This exists so we can store entity effects in list and raise events without type erasure.
[ImplicitDataDefinitionForInheritors]
public abstract partial class EntityEffect
{
    public abstract void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale);

    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// If our scale is less than this value, the effect fails.
    /// </summary>
    [DataField]
    public float MinScale;

    /// <summary>
    /// If true, then it allows the scale multiplier to go above 1.
    /// </summary>
    [DataField]
    public bool Scaling;

    // TODO: This should be an entity condition but guidebook relies on it heavily for formatting...
    [DataField]
    public float Probability = 1.0f;

    /// <summary>
    /// A general description of the entity effect for guidebooks.
    /// </summary>
    protected virtual string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    // TODO: Move this to reagent prototype?
    public virtual string EntityEffectFormat => "guidebook-reagent-effect-description";

    public virtual string? GuidebookEffectDescription(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (EntityEffectGuidebookText(prototype, entSys) is not { } effect)
            return null;

        // TODO: Min Scale and EntityConditions!!!
        return Loc.GetString(
            EntityEffectFormat,
            ("effect", effect),
            ("chance", Probability),
            ("conditionCount", Conditions?.Length ?? 0),
            ("conditions",
                ContentLocalizationManager.FormatList(
                    Conditions?.Select(x => x.EntityConditionGuidebookText(prototype)).ToList() ?? new List<string>()
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
[ByRefEvent, Access(typeof(SharedEntityEffectsSystem))]
public readonly record struct EntityEffectEvent<T>(T Effect, float Scale) where T : EntityEffectBase<T>
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
