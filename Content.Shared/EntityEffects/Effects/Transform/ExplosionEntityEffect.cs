using Content.Shared.Database;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <inheritdoc cref="EntityEffect"/>
/// <seealso cref="Explode"/>
public sealed partial class Explosion : EntityEffectBase<Explosion>
{
    /// <summary>
    ///     The type of explosion. Determines damage types and tile break chance scaling.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ExplosionPrototype> ExplosionType;

    /// <summary>
    ///     The max intensity the explosion can have at a given tile. Places an upper limit of damage and tile break
    ///     chance.
    /// </summary>
    [DataField]
    public float MaxIntensity = 5;

    /// <summary>
    ///     How quickly intensity drops off as you move away from the epicenter
    /// </summary>
    [DataField]
    public float IntensitySlope = 1;

    /// <summary>
    ///     The maximum total intensity that this chemical reaction can achieve. Basically here to prevent people
    ///     from creating a nuke by collecting enough potassium and water.
    /// </summary>
    /// <remarks>
    ///     A slope of 1 and MaxTotalIntensity of 100 corresponds to a radius of around 4.5 tiles.
    /// </remarks>
    [DataField]
    public float MaxTotalIntensity = 100;

    /// <summary>
    ///     The intensity of the explosion per unit reaction.
    /// </summary>
    [DataField]
    public float IntensityPerUnit = 1;

    /// <summary>
    ///     Factor used to scale the explosion intensity when calculating tile break chances. Allows for stronger
    ///     explosives that don't space tiles, without having to create a new explosion-type prototype.
    /// </summary>
    [DataField]
    public float TileBreakScale = 1f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-explosion", ("chance", Probability));

    public override LogImpact? Impact => LogImpact.High;
}
