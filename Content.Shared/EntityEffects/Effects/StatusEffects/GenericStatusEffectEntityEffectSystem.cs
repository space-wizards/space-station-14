using Content.Shared.StatusEffect;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

// TODO: Old status is obsolete, this is here cause not everything has been moved over yet.
public sealed partial class GenericStatusEffectEntityEffectSystem : EntityEffectSystem<MetaDataComponent, GenericStatusEffect>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<GenericStatusEffect> args)
    {
        var time = args.Effect.Time * args.Scale;

        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Add:
                if (args.Effect.Component != String.Empty)
                    _status.TryAddStatusEffect(entity, args.Effect.Key, TimeSpan.FromSeconds(time), args.Effect.Refresh, args.Effect.Component);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, args.Effect.Key, TimeSpan.FromSeconds(time));
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetTime(entity, args.Effect.Key, TimeSpan.FromSeconds(time));
                break;
        }
    }
}

[DataDefinition]
public sealed partial class GenericStatusEffect : EntityEffectBase<GenericStatusEffect>
{
    [DataField(required: true)]
    public string Key = default!;

    [DataField]
    public string Component = String.Empty;

    [DataField]
    public float Time = 2.0f;

    /// <remarks>
    ///     true - refresh status effect time,  false - accumulate status effect time
    /// </remarks>
    [DataField]
    public bool Refresh = true;

    /// <summary>
    ///     Should this effect add the status effect, remove time from it, or set its cooldown?
    /// </summary>
    [DataField]
    public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Add;
}
