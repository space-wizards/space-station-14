using Content.Shared.Destructible.Thresholds;
using Content.Shared.Xenoarchaeology.Artifact;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// This is used for recharging all nearby batteries when activated.
/// </summary>
[RegisterComponent, Access(typeof(XAEChargeBatterySystem))]
public sealed partial class XAEChargeBatteryComponent : Component
{
    /// <summary>
    /// The radius of entities that will be affected. Can be affected by modifiers <see cref="XenoArtifactEffectModifier.Range"/>.
    /// Always restricted by <see cref="RadiusRestrictions"/>.
    /// </summary>
    [DataField]
    public float DefaultRadius = 15f;

    /// <summary>
    /// Min and max radius of the effect (restricting default and modifiers).
    /// </summary>
    [DataField]
    public MinMax RadiusRestrictions = new(5f, 40f);

    /// <summary>
    /// Amount of charge to be added by effect activation.
    /// </summary>
    [DataField]
    public float AddChargeAmount = 400f;

    /// <summary>
    /// Min and max amount of charge, effect can give off.
    /// </summary>
    [DataField]
    public MinMax ChargeAmountRestrictions = new(50f, 10000000f);
}
