using Content.Shared.EntityEffects;
using Content.Server.Flash;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

[DataDefinition]
public sealed partial class FlashReactionEffect : EntityEffect
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
    ///     The time entities will be flashed in seconds.
    ///     The default is chosen to be better than the hand flash so it is worth using it for grenades etc.
    /// </summary>
    [DataField]
    public float Duration = 4f;

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

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-flash-reaction-effect", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var transform = args.EntityManager.GetComponent<TransformComponent>(args.TargetEntity);
        var transformSystem = args.EntityManager.System<SharedTransformSystem>();

        var range = 1f;

        if (args is EntityEffectReagentArgs reagentArgs)
            range = MathF.Min((float)(reagentArgs.Quantity * RangePerUnit), MaxRange);

        args.EntityManager.System<FlashSystem>().FlashArea(
            args.TargetEntity,
            null,
            range,
            Duration * 1000,
            slowTo: SlowTo,
            sound: Sound);

        if (FlashEffectPrototype == null)
            return;

        var uid = args.EntityManager.SpawnEntity(FlashEffectPrototype, transformSystem.GetMapCoordinates(transform));
        transformSystem.AttachToGridOrMap(uid);

        if (!args.EntityManager.TryGetComponent<PointLightComponent>(uid, out var pointLightComp))
            return;
        var pointLightSystem = args.EntityManager.System<SharedPointLightSystem>();
        // PointLights with a radius lower than 1.1 are too small to be visible, so this is hardcoded
        pointLightSystem.SetRadius(uid, MathF.Max(1.1f, range), pointLightComp);
    }
}
