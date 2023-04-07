namespace Content.Client.Power.APC;

[RegisterComponent]
[Access(typeof(ApcVisualizerSystem))]
public sealed class ApcVisualsComponent : Component
{
    /// <summary>
    /// The base of all APC sprite states.
    /// </summary>
    [DataField("spriteStateBase")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string SpriteStateBase = "apc";
    
#region Overlays

#region Indicators

#region Locks

    /// <summary>
    /// The number of lock indicators on the APC.
    /// </summary>
    [DataField("numLockIndicators")]
    [ViewVariables(VVAccess.ReadWrite)]
    public byte LockIndicators = 2;

    /// <summary>
    /// The prefix used for the sprite state suffix of the interface lock indicator light following the base sprite state.
    /// Valid states are of the form \<BASE\>\<PREFIX\>\<IDX>\-\<STATE\>
    /// </summary>
    [DataField("lockIndicatorPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string LockPrefix = "ox";

    /// <summary>
    /// The shader used for the interface lock indicator light.
    /// </summary>
    [DataField("lockIndicatorShader")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? LockShader = "unshaded";

#endregion Locks

#region Channels

    /// <summary>
    /// The number of output channel indicator lights on the APC.
    /// </summary>
    [DataField("numChannelIndicators")]
    [ViewVariables(VVAccess.ReadWrite)]
    public byte ChannelIndicators = 3;

    /// <summary>
    /// The prefix used for the sprite state suffix of the channel indicator lights following the base sprite state.
    /// Valid states are of the form \<BASE\>\<PREFIX\>\<IDX\>-\<STATE\>
    /// </summary>
    [DataField("channelIndicatorPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ChannelPrefix = "o";

    /// <summary>
    /// 
    /// </summary>
    [DataField("channelIndicatorShader")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ChannelShader = "unshaded";

#endregion Channels

#endregion Indicators

#region Screen

    /// <summary>
    /// The prefix used to construct the sprite state suffix used for the screen overlay.
    /// Valid sprite states are of the form \<BASE\>\<PREFIX\>-\<STATE\>.
    /// </summary>
    [DataField("screenStatePrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ScreenPrefix = "o3";

    /// <summary>
    /// The shader used for the screen overlay.
    /// </summary>
    [DataField("screenShader")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ScreenShader = "unshaded";

    /// <summary>
    /// The sprite state of the unlit overlay used for the APC screen when the APC has been emagged.
    /// </summary>
    [DataField("emaggedScreenState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string EmaggedScreenState = "emag-unlit";

#region Light

    /// <summary>
    /// The color of the light emitted by the APC when there is not enough power in the attached network for the APC to charge.
    /// Picked to match the color of the unshaded screen overlay used by the APC while in this state.
    /// </summary>
    [DataField("lackColor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color LackColor = Color.FromHex("#d1332e");
    /// <summary>
    /// The color of the light emitted by the APC when the APC is charging via the attached network.
    /// Picked to match the color of the unshaded screen overlay used by the APC while in this state.
    /// </summary>
    [DataField("chargingColor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color ChargingColor = Color.FromHex("#2e8ad1");
    /// <summary>
    /// The color of the light emitted by the APC when the APC is fully charged.
    /// Picked to match the color of the unshaded screen overlay used by the APC while in this state.
    /// </summary> 
    [DataField("fullColor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color FullColor = Color.FromHex("#3db83b");
    /// <summary>
    /// The color of the light emitted by the APC when it has been emagged.
    /// Picked to match the color of the unshaded screen overlay used by the APC while in this state.
    /// </summary>
    [DataField("emagColor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color EmagColor = Color.FromHex("#1f48d6");

#endregion Light

#endregion Screen

#endregion Overlays
}
