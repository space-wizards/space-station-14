using Content.Shared.Flash;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Creates a Flash at this entity's coordinates.
/// Range is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class FlashEntityEffectSystem : EntityEffectSystem<TransformComponent, Flash>
{
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<Flash> args)
    {
        var range = MathF.Min(args.Scale * args.Effect.RangePerUnit, args.Effect.MaxRange);

        _flash.FlashArea(
            entity,
            null,
            range,
            args.Effect.Duration,
            slowTo: args.Effect.SlowTo,
            sound: args.Effect.Sound);

        if (args.Effect.FlashEffectPrototype == null)
            return;

        var uid = PredictedSpawnAtPosition(args.Effect.FlashEffectPrototype, _xform.GetMoverCoordinates(entity));

        _pointLight.SetRadius(uid, MathF.Max(1.1f, range));
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Flash : EntityEffectBase<Flash>
{
    /// <summary>
    ///     Flash range per unit of reagent.
    /// </summary>
    [DataField]
    public float RangePerUnit = 0.2f;

    /// <summary>
    ///     Maximum flash range.
    /// </summary>
    [DataField]
    public float MaxRange = 10f;

    /// <summary>
    ///     How much to entities are slowed down.
    /// </summary>
    [DataField]
    public float SlowTo = 0.5f;

    /// <summary>
    ///     The time entities will be flashed.
    ///     The default is chosen to be better than the hand flash so it is worth using it for grenades etc.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(4);

    /// <summary>
    ///     The prototype ID used for the visual effect.
    /// </summary>
    [DataField]
    public EntProtoId? FlashEffectPrototype = "ReactionFlash";

    /// <summary>
    ///     The sound the flash creates.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-flash-reaction-effect", ("chance", Probability));
}
