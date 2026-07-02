using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects;

public readonly record struct EntityEffectData(EntityEffect Effect, float Scale, EntityUid? User)
{
    public static implicit operator EntityEffectData((EntityEffect effect, float scale, EntityUid? user) tuple)
    {
        return new EntityEffectData(tuple.effect, tuple.scale, tuple.user);
    }
}

/// <summary>
/// This handles entity effects.
/// Specifically it handles the receiving of events for causing entity effects, and provides
/// public API for other systems to take advantage of entity effects.
/// </summary>
public sealed partial class SharedEntityEffectsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;

    private Dictionary<Type, EntityEffectHandler> _handlers = new();

    public void RegisterHandler(EntityEffectHandler handler)
    {
        _handlers[handler.EffectType] = handler;
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<ReactiveComponent, ReactionEntityEvent>(OnReactive);
    }

    private void OnReactive(Entity<ReactiveComponent> entity, ref ReactionEntityEvent args)
    {
        var scale = args.ReagentQuantity.Quantity.Float();

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

                ApplyEffects(entity, val.Effects, scale);
            }
        }

        if (entity.Comp.Reactions != null)
        {
            foreach (var entry in entity.Comp.Reactions)
            {
                if (!entry.Methods.Contains(args.Method))
                    continue;

                if (entry.Reagents == null || entry.Reagents.Contains(args.Reagent.ID))
                    ApplyEffects(entity, entry.Effects, scale);
            }
        }
    }

    /// <inheritdoc cref="ApplyEffects{T}(EntityUid,T[],float,EntityUid?)"/>
    public void ApplyEffects<T>(EntityUid target, T[] effects, FixedPoint2 scale, EntityUid? user = null) where T : EntityEffect
    {
        ApplyEffects(target, effects, scale.Float());
    }

    /// <summary>
    /// Applies a list of entity effects to a target entity.
    /// </summary>
    /// <param name="target">Entity being targeted by the effects</param>
    /// <param name="effects">Effects we're applying to the entity</param>
    /// <param name="scale">Optional scale multiplier for the effects</param>
    /// <param name="user">The entity causing the effect.</param>
    public void ApplyEffects<T>(EntityUid target, T[] effects, float scale = 1f, EntityUid? user = null) where T : EntityEffect
    {
        // do all effects, if conditions apply
        foreach (var effect in effects)
        {
            TryApplyEffect(target, effect, scale, user);
        }
    }

    /// <summary>
    /// Applies an entity effect to a target if all conditions pass.
    /// </summary>
    /// <param name="target">Target we're applying an effect to</param>
    /// <param name="effect">Effect we're applying</param>
    /// <param name="scale">Optional scale multiplier for the effect.</param>
    /// <param name="user">The entity causing the effect.</param>
    /// <returns>True if all conditions pass!</returns>
    public bool TryApplyEffect<T>(EntityUid target, T effect, float scale = 1f, EntityUid? user = null) where T : EntityEffect
    {
        if (scale < effect.MinScale)
            return false;

        // TODO: Replace with proper random prediciton when it exists.
        if (effect.Probability <= 1f && !SharedRandomExtensions.PredictedProb(_timing, effect.Probability, GetNetEntity(target), GetNetEntity(user)))
            return false;

        // See if conditions apply
        if (!_condition.TryConditions(target, effect.Conditions))
            return false;

        ApplyEffect(target, effect, scale, user);
        return true;
    }

    /// <summary>
    /// Applies an <see cref="EntityEffect"/> to a given target.
    /// This doesn't check conditions so you should only call this if you know what you're doing!
    /// </summary>
    /// <param name="target">Target we're applying an effect to</param>
    /// <param name="effect">Effect we're applying</param>
    /// <param name="scale">Optional scale multiplier for the effect.</param>
    /// <param name="user">The entity causing the effect.</param>
    public void ApplyEffect<T>(EntityUid target, T effect, float scale = 1f, EntityUid? user = null) where T : EntityEffect
    {
        // Clamp the scale if the effect doesn't allow scaling.
        if (!effect.Scaling)
            scale = Math.Min(scale, 1f);

        if (effect.Impact is { } level)
        {
            _adminLog.Add(
                effect.LogType,
                level,
                $"Entity effect {effect.GetType().Name:effect}"
                + $" applied on entity {target:entity}"
                + $" at {Transform(target).Coordinates:coordinates}"
                + $" with a scale multiplier of {scale}"
            );
        }

        if (_handlers.TryGetValue(effect.GetType(), out var handler))
            handler.ApplyEffect(target,(effect, scale, user));
    }
}

/// <summary>
/// Abstract base class for entity effect handlers.
/// Extends EntitySystem so concrete handlers are proper engine systems.
/// </summary>
public abstract partial class EntityEffectHandler : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public abstract Type EffectType { get; }
    public abstract void ApplyEffect(EntityUid target, EntityEffectData args);

    /// <inheritdoc/>
    public override void Initialize()
    {
        _effects.RegisterHandler(this);
    }
}

/// <summary>
/// This is a basic abstract entity effect containing all the data an entity effect needs to affect entities with effects...
/// </summary>
/// <typeparam name="T">The Component that is required for the effect</typeparam>
/// <typeparam name="TEffect">The Entity Effect itself</typeparam>
public abstract partial class EntityEffectSystem<T, TEffect> : EntityEffectHandler
    where T : Component where TEffect : EntityEffect
{
    [Dependency] private EntityQuery<T> _query = default!;

    public override Type EffectType => typeof(TEffect);

    protected abstract void Effect(Entity<T> entity, TEffect effect, EntityEffectData data);

    public override void ApplyEffect(EntityUid target, EntityEffectData args)
    {
        if (args.Effect is not TEffect typed)
            return;
        if (!_query.TryGetComponent(target, out var comp))
            return;
        Effect((target, comp), typed, args);
    }
}
