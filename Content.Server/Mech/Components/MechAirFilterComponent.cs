using Content.Shared.Atmos;

namespace Content.Server.Mech.Components;

/// <summary>
/// This is basically a reverse scrubber for MechAir
/// </summary>
[RegisterComponent]
public sealed partial class MechAirFilterComponent : Component
{
    /// <summary>
    /// Gases that will be filtered out of internal air
    /// </summary>
    [DataField("gases", required: true)]
    public HashSet<Gas> Gases = new();

    /// <summary>
    /// Gases that will be filtered when above <see cref="OverflowPressure"/>.
    /// Replaces <see cref="Gases"/> when overflowing.
    /// </summary>
    /// <remarks>
    /// This is intended for nitrogen to be removed at full pressure so as more
    /// air is replaced oxygen doesn't tend towards 0 as more nitrogen is added
    /// but not breathed by the animal.
    /// </remarks>
    [DataField("overflowGases", required: true)]
    public HashSet<Gas> OverflowGases = new();

    /// <summary>
    /// Pressure to filter <see cref="OverflowGases"/> at.
    /// </summary>
    [DataField("overflowPressure"), ViewVariables(VVAccess.ReadWrite)]
    public float OverflowPressure = 100f;

    /// <summary>
    /// Target volume to transfer every second.
    /// </summary>
    [DataField("transferRate"), ViewVariables(VVAccess.ReadWrite)]
    public float TransferRate = MechAirComponent.GasMixVolume * 0.2f;
}
