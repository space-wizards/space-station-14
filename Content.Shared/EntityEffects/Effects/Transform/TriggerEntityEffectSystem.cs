using Content.Shared.Trigger.Systems;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Triggers the entity's trigger component.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TriggerEntityEffectSystem : EntityEffectSystem<TransformComponent, Trigger>
{
    [Dependency] private TriggerSystem _trigger = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<Trigger> args)
    {
        _trigger.Trigger(entity, args.User, args.Effect.KeyOut);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Trigger : EntityEffectBase<Trigger>
{
    /// <summary>
    /// The trigger key to use when triggering.
    /// </summary>
    [DataField]
    public string? KeyOut { get; set; } = TriggerSystem.DefaultTriggerKey;
}
