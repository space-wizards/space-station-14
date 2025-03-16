using Content.Shared.APC;

namespace Content.Client.Power.APC;

[RegisterComponent]
[Access(typeof(ApcVisualizerSystem))]
public sealed partial class ApcVisualsComponent : Component
{
#region Indicators

#region Locks

    /// <summary>
    /// The number of lock indicators on the APC.
    /// </summary>
    [DataField("numLockIndicators")]
    [ViewVariables(VVAccess.ReadWrite)]
    public byte LockIndicators = 2;

    /// <summary>
    /// The prefix used for the sprite state suffix of the lock indicator lights.
    /// Valid states are of the form \<BASE\>\<PREFIX\>\<IDX>\-\<STATE\>
    /// </summary>
    [DataField("lockIndicatorPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string LockPrefix = "lock";

    /// <summary>
    /// The suffixes used for the sprite state suffix of the lock indicator lights.
    /// Valid states are of the form \<PREFIX\>\<IDX\>-\<STATE\>
    /// </summary>
    [DataField("lockIndicatorSuffixes")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string[] LockSuffixes = new string[(byte)(2 << (sbyte)ApcLockState.LogWidth)]{"unlocked", "locked"};

#endregion Locks

#region Channels

    /// <summary>
    /// The number of output channel indicator lights on the APC.
    /// </summary>
    [DataField("numChannelIndicators")]
    [ViewVariables(VVAccess.ReadWrite)]
    public byte ChannelIndicators = 3;

    /// <summary>
    /// The prefix used for the sprite state suffix of the channel indicator lights.
    /// Valid states are of the form \<BASE\>\<PREFIX\>\<IDX\>-\<STATE\>
    /// </summary>
    [DataField("channelIndicatorPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ChannelPrefix = "channel";

    /// <summary>
    /// The suffixes used for the sprite state suffix of the channel indicator lights.
    /// Valid states are of the form \<PREFIX\>\<IDX\>-\<STATE\>
    /// </summary>
    [DataField("channelIndicatorSuffixes")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string[] ChannelSuffixes = new string[(byte)(2 << (sbyte)ApcChannelState.LogWidth)]{"auto_off", "manual_off", "auto_on", "manual_on"};

#endregion Channels

#endregion Indicators

#region Screen

    /// <summary>
    /// The prefix used to construct the sprite state suffix used for the screen overlay.
    /// Valid sprite states are of the form \<PREFIX\>-\<SUFFIX\>.
    /// </summary>
    [DataField("screenStatePrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ScreenPrefix = "display";

    /// <summary>
    /// The suffix used to construct the sprite state suffix used for the screen overlay.
    /// Valid sprite states are of the form \<PREFIX\>-\<STATE\>.
    /// </summary>
    [DataField("screenStateSuffixes")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string[] ScreenSuffixes = new string[(byte)ApcChargeState.NumStates]{"lack", "charging", "full", "remote"};

    /// <summary>
    /// The colors of the light emitted by the APC given a particular display state.
    /// </summary>
    [DataField("screenColors")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color[] ScreenColors = new Color[(byte)ApcChargeState.NumStates]{Color.FromHex("#d1332e"), Color.FromHex("#dcdc28"), Color.FromHex("#82ff4c"), Color.FromHex("#ffac1c")};

    /// <summary>
    /// The sprite state of the unlit overlay used for the APC screen when the APC has been emagged.
    /// </summary>
    [DataField("emaggedScreenState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string EmaggedScreenState = "emag-unlit";

    /// <summary>
    /// The color of the light emitted when the APC has been emagged.
    /// </summary>
    [DataField("emaggedScreenColor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color EmaggedScreenColor = Color.FromHex("#1f48d6");

#endregion Screen
}
