using Content.Shared.Database;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

/// <summary>
/// A basic instantaneous effect which can be applied to an entity via events.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class EntityEffect
{
    /// <summary>
    /// Applies this effect to a target.
    /// </summary>
    /// <param name="target">Target we're applying an effect to.</param>
    /// <param name="raiser">The type of effect raising the event.</param>
    /// <param name="scale">Optional scale multiplier for the effect.</param>
    /// <param name="user">The entity causing the effect.</param>
    public abstract void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user);

    /// <summary>
    /// Conditions for this effect to happen.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// If our scale is less than this value, the effect fails.
    /// </summary>
    [DataField]
    public virtual float MinScale { get; private set; }

    /// <summary>
    /// If true, then it allows the scale multiplier to go above 1.
    /// </summary>
    [DataField]
    public virtual bool Scaling { get; private set; } = true;

    // TODO: This should be an entity condition but guidebook relies on it heavily for formatting...
    /// <summary>
    /// Probability of the effect occuring.
    /// </summary>
    [DataField]
    public float Probability = 1.0f;

/// <summary>
/// Generates the guidebook text for this effect.
/// </summary>
/// <param name="prototype">Prototype manager, to resolve prototype calls.</param>
/// <param name="entSys">EntitySystem manager, to resolve system calls.</param>
/// <returns>The guidebook text string, if generated. Null otherwise.</returns>
    public virtual string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    /// <summary>
    /// If this effect is logged, how important is the log?
    /// </summary>
    [ViewVariables]
    public virtual LogImpact? Impact => null;

    /// <summary>
    /// The type of log this effect should cause.
    /// </summary>
    [ViewVariables]
    public virtual LogType LogType => LogType.EntityEffect;
}

/// <summary>
/// Used to store an <see cref="EntityEffect"/> so it can be raised without losing the type of the condition.
/// </summary>
/// <typeparam name="T">The Condition wer are raising.</typeparam>
public abstract partial class EntityEffectBase<T> : EntityEffect where T : EntityEffectBase<T>
{
    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        if (this is not T type)
            return;

        raiser.RaiseEffectEvent(target, type, scale, user);
    }
}
