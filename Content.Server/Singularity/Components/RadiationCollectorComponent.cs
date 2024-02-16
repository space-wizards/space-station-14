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
    ///     Power output (in Watts) per unit of radiation collected.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ChargeModifier = 30000f;

    /// <summary>
    ///     Number of power ticks that the power supply can remain active for. This is needed since
    ///     power and radiation don't update at the same tickrate, and since radiation does not provide
    ///     an update when radiation is removed. When this goes to zero, zero out the power supplier
    ///     to model the radiation source going away.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int PowerTicksLeft = 0;

    /// <summary>
    ///     Is the machine enabled.
    /// </summary>
    [DataField]
    [ViewVariables]
    public bool Enabled;

    /// <summary>
    ///     List of gases that will react to the radiation passing through the collector
    /// </summary>
    [DataField]
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
    [DataField(required: true)]
    public Gas ReactantPrototype;

    /// <summary>
    ///     Multipier for the amount of power produced by the radiation collector when using this gas
    /// </summary>
    [DataField]
    public float PowerGenerationEfficiency = 1f;

    /// <summary>
    ///     Controls the rate (molar percentage per rad) at which the reactant breaks down when exposed to radiation
    /// </summary>
    /// /// <remarks>
    ///     Set to zero if the reactant does not deplete
    /// </remarks>
    [DataField]
    public float ReactantBreakdownRate = 1f;

    /// <summary>
    ///     A byproduct gas that is generated when the reactant breaks down
    /// </summary>
    /// <remarks>
    ///     Leave null if the reactant no byproduct gas is to be formed
    /// </remarks>
    [DataField]
    public Gas? Byproduct;

    /// <summary>
    ///     The molar ratio of the byproduct gas generated from the reactant gas
    /// </summary>
    [DataField]
    public float MolarRatio = 1f;
}
