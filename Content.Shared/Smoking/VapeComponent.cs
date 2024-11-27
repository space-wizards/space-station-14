using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Smoking;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSmokingSystem))]
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
    /// Gas that the vape will release.
    /// </summary>
    [DataField]
    public Gas GasType = Gas.WaterVapor;

    /// <summary>
    /// Solution volume will be divided by this number and converted to the gas.
    /// </summary>
    [DataField]
    public float ReductionFactor = 300f;
}
