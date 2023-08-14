using Content.Shared.Atmos;

namespace Content.Server.Mech.Components;

/// <summary>
/// This is basically a reverse scrubber for MechAir
/// </summary>
[RegisterComponent]
public sealed class MechAirFilterComponent : Component
{
    /// <summary>
    /// Gases that will be filtered out of internal air
    /// </summary>
    [DataField("gases")]
    public HashSet<Gas> Gases = new HashSet<Gas>
    {
        Gas.CarbonDioxide,
        Gas.Plasma,
        Gas.Tritium,
        Gas.WaterVapor,
        Gas.Miasma,
        Gas.NitrousOxide,
        Gas.Frezon
        //Gas.Helium4
    };

    /// <summary>
    /// Target volume to transfer every second.
    /// </summary>
    [DataField("transferRate")]
    public float TransferRate = Atmospherics.MaxTransferRate;
}
