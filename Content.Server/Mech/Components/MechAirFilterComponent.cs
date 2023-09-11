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
    /// Target volume to transfer every second.
    /// </summary>
    [DataField("transferRate")]
    public float TransferRate = MechAirComponent.GasMixVolume * 0.1f;
}
