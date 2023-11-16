using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Atmos.Miasma;

/// <summary>
/// This makes mobs eventually start rotting when they die.
/// It may be expanded to food at some point, but it's just for mobs right now.
/// </summary>
[RegisterComponent]
public sealed partial class PerishableComponent : Component
{
    /// <summary>
    /// How long it takes after death to start rotting.
    /// </summary>
    [DataField("rotAfter"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RotAfter = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How much rotting has occured
    /// </summary>
    [DataField("rotAccumulator"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RotAccumulator = TimeSpan.Zero;

    /// <summary>
    /// Gasses are released, this is when the next gas release update will be.
    /// </summary>
    [DataField("rotNextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPerishUpdate = TimeSpan.Zero;

    /// <summary>
    /// How often the rotting ticks.
    /// Feel free to tweak this if there are perf concerns.
    /// </summary>
    [DataField("perishUpdateRate"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PerishUpdateRate = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How many moles of gas released per second, per unit of mass.
    /// </summary>
    [DataField("molsPerSecondPerUnitMass"), ViewVariables(VVAccess.ReadWrite)]
    public float MolsPerSecondPerUnitMass = 0.0025f;
}


[ByRefEvent]
public record struct IsRottingEvent(bool Handled = false);
