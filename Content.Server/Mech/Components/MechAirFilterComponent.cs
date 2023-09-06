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
    /// Gases that will be filtered out of internal air to maintain oxygen ratio.
    /// When oxygen is below <see cref="TargetOxygen"/>, these gases will be filtered instead of <see cref="Gases"/>.
    /// </summary>
    [DataField("overflowGases", required: true)]
    public HashSet<Gas> OverflowGases = new();

    /// <summary>
    /// Minimum oxygen fraction before it will start removing <see cref="OverflowGases"/>.
    /// </summary>
    [DataField("targetOxygen"), ViewVariables(VVAccess.ReadWrite)]
    public float TargetOxygen = 0.21f;

    /// <summary>
    /// Gas to consider oxygen for <see cref="TargetOxygen"/> and <see cref="OverflowGases"/> logic.
    /// </summary>
    /// <remarks>
    /// If you make a slime mech you might want to change this to be nitrogen, and overflowgases to remove oxygen.
    /// However theres still no real danger since standard atmos is mostly nitrogen so nitrogen tends to 100% anyway.
    /// </remarks>
    [DataField("oxygen"), ViewVariables(VVAccess.ReadWrite)]
    public Gas Oxygen = Gas.Oxygen;

    /// <summary>
    /// Target volume to transfer every second.
    /// </summary>
    [DataField("transferRate"), ViewVariables(VVAccess.ReadWrite)]
    public float TransferRate = MechAirComponent.GasMixVolume * 0.1f;
}
