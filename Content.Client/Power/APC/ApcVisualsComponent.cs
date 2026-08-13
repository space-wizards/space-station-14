using Content.Shared.APC;

namespace Content.Client.Power.APC;

/// <summary>
/// A component used to update the sprite of APCs.
/// </summary>
/// <seealso cref="ApcComponent"/>
[RegisterComponent]
[Access(typeof(ApcVisualizerSystem))]
public sealed partial class ApcVisualsComponent : Component
{
    #region Channel Indicators

    /// <summary>
    /// The prefix used for the sprite state suffix of the channel indicator lights.
    /// Valid states are of the form \<PREFIX\>-\<STATE\>
    /// </summary>
    [DataField("channelIndicatorPrefix")]
    public string ChannelPrefix = "channel";

    /// <summary>
    /// The suffixes are used for the channel indicator lights.
    /// Must be at least as large as <see cref="ApcChannelState.NumStates"/>
    /// A null state is not used as a suffix, it sets the entire state to null.
    /// </summary>
    [DataField("channelIndicatorSuffixes")]
    public string?[] ChannelSuffixes = new string?[(byte)ApcChannelState.NumStates] { null, "on", "disconnected", "tripped" };

    #endregion Channel Indicators

    #region Screen

    /// <summary>
    /// The prefix used to construct the sprite state suffix used for the screen overlay.
    /// Valid sprite states are of the form \<PREFIX\>-\<SUFFIX\>.
    /// </summary>
    [DataField("screenStatePrefix")]
    public string ScreenPrefix = "display";

    /// <summary>
    /// The suffix used to construct the sprite state suffix used for the screen overlay.
    /// Valid sprite states are of the form \<PREFIX\>-\<STATE\>.
    /// Must be at least as large as <see cref="ApcChargeState.NumStates"/>
    /// A null state is not used as a suffix, it sets the entire state to null.
    /// </summary>
    [DataField("screenStateSuffixes")]
    public string?[] ScreenSuffixes = new string[(byte)ApcChargeState.NumStates] { "lack", "charging", "full", "remote", "tripped" };

    /// <summary>
    /// The colors of the light emitted by the APC given a particular display state.
    /// Must be at least as large as <see cref="ApcChargeState.NumStates"/>
    /// </summary>
    [DataField]
    public Color[] ScreenColors = new Color[(byte)ApcChargeState.NumStates] { Color.FromHex("#d1332e"), Color.FromHex("#dcdc28"), Color.FromHex("#82ff4c"), Color.FromHex("#ffac1c"), Color.FromHex("#cccccc") };

    /// <summary>
    /// The sprite state of the unlit overlay used for the APC screen when the APC has been emagged.
    /// </summary>
    [DataField]
    public string EmaggedScreenState = "emag-unlit";

    /// <summary>
    /// The color of the light emitted when the APC has been emagged.
    /// </summary>
    [DataField]
    public Color EmaggedScreenColor = Color.FromHex("#1f48d6");

    #endregion Screen
}
