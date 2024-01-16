namespace Content.Server.Power.Generator;

/// <summary>
/// When attached to an entity with <see cref="FuelGeneratorComponent"/> it will allow the signal network to exert control over the generator.
/// </summary>
[RegisterComponent]
public sealed partial class GeneratorSignalControlComponent: Component
{
    /// <summary>
    /// The port that should be invoked when turning the generator on.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string OnPort = "On";

    /// <summary>
    /// The port that should be invoked when turning the generator off.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string OffPort = "Off";

    /// <summary>
    /// The port that should be invoked when toggling the generator.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string TogglePort = "Toggle";
}
