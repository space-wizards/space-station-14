using Content.Shared.Power.Generator;

namespace Content.Server.Power.Generator;

/// <summary>
/// Responsible for power output switching &amp; UI logic on portable generators.
/// </summary>
/// <remarks>
/// A portable generator is expected to have the following components: <see cref="SolidFuelGeneratorAdapterComponent"/>, <see cref="FuelGeneratorComponent"/>.
/// </remarks>
/// <seealso cref="PortableGeneratorSystem"/>
[RegisterComponent]
[Access(typeof(PortableGeneratorSystem))]
public sealed class PortableGeneratorComponent : Component
{
    /// <summary>
    /// Which output the portable generator is currently connected to.
    /// </summary>
    [DataField("activeOutput")]
    public PortableGeneratorPowerOutput ActiveOutput { get; set; }
}

/// <summary>
/// Possible power output for portable generators.
/// </summary>
/// <seealso cref="PortableGeneratorComponent"/>
public enum PortableGeneratorPowerOutput : byte
{
    /// <summary>
    /// The generator is set to connect to a high-voltage power network.
    /// </summary>
    HV,

    /// <summary>
    /// The generator is set to connect to a medium-voltage power network.
    /// </summary>
    MV
}
