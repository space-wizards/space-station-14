using Content.Server.Singularity.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Singularity.Components;

/// <summary>
///     Generates electricity from radiation.
/// </summary>
[RegisterComponent]
[Access(typeof(RadiationCollectorSystem))]
public sealed partial class RadiationCollectorComponent : Component
{
    /// <summary>
    ///     How much joules will collector generate for each rad.
    /// </summary>
    [DataField("chargeModifier")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ChargeModifier = 30000f;

    /// <summary>
    ///     Cooldown time between users interaction.
    /// </summary>
    [DataField("cooldown")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.81f);

    /// <summary>
    ///     Was machine activated by user?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Enabled;

    /// <summary>
    ///     Timestamp when machine can be deactivated again.
    /// </summary>
    public TimeSpan CoolDownEnd;

    /// <summary>
    ///     List of gases that will react to the radiation passing through the collector
    /// </summary>
    [DataField("radiationReactiveGases")]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<RadiationReactiveGas>? RadiationReactiveGases;
}

/// <summary>
///     Describes how a gas reacts to the collected radiation
/// </summary>
[DataDefinition]
public sealed partial class RadiationReactiveGas
{
    /// <summary>
    ///     The reactant gas 
    /// </summary>
    [DataField("reactantPrototype", required: true)]
    public Gas Reactant = Gas.Plasma;

    /// <summary>
    ///     Multipier for the amount of power produced by the radiation collector when using this gas
    /// </summary>
    [DataField("powerGenerationEfficiency")]
    public float PowerGenerationEfficiency = 1f;

    /// <summary>
    ///     Controls the rate (molar percentage per rad) at which the reactant breaks down when exposed to radiation
    /// </summary>
    /// /// <remarks>
    ///     Set to zero if the reactant does not deplete
    /// </remarks>
    [DataField("reactantBreakdownRate")]
    public float ReactantBreakdownRate = 1f;

    /// <summary>
    ///     A byproduct gas that is generated when the reactant breaks down
    /// </summary>
    /// <remarks>
    ///     Leave null if the reactant no byproduct gas is to be formed
    /// </remarks>
    [DataField("byproductPrototype")]
    public Gas? Byproduct = null;

    /// <summary>
    ///     The molar ratio of the byproduct gas generated from the reactant gas
    /// </summary>
    [DataField("molarRatio")]
    public float MolarRatio = 1f;
}
