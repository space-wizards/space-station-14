using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Damage;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(SmokingSystem))]
public sealed partial class VapeComponent : Component
{
    /// <summary>
    /// Do after delay for a forced puff.
    /// </summary>
    [DataField]
    public float Delay = 5;

    /// <summary>
    /// Do after delay for a puff.
    /// </summary>
    [DataField]
    public float UserDelay = 2;

    /// <summary>
    /// Intensity of a vape explosion.
    /// </summary>
    [DataField]
    public float ExplosionIntensity = 2.5f;

    /// <summary>
    /// Vape damage to lungs.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Gas that vape will release.
    /// </summary>
    [DataField]
    public Gas GasType = Gas.WaterVapor;

    /// <summary>
    /// Solution volume will be divided by this number and converted to the gas.
    /// </summary>
    [DataField]
    public float ReductionFactor = 300f;
}
