using Content.Shared.VendingMachines;

namespace Content.Client.VendingMachines;

[RegisterComponent]
[ComponentReference(typeof(SharedVendingMachineComponent))]
[Access(typeof(VendingMachineSystem))]
public sealed class VendingMachineComponent : SharedVendingMachineComponent
{
    /// <summary>
    /// RSI state for when the vending machine is unpowered.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
    /// </summary>
    [DataField("offState")]
    public string? OffState;

    /// <summary>
    /// RSI state for the screen of the vending machine
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Screen"/>
    /// </summary>
    [DataField("screenState")]
    public string? ScreenState;

    /// <summary>
    /// RSI state for the vending machine's normal state. Usually a looping animation.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
    /// </summary>
    [DataField("normalState")]
    public string? NormalState;

    /// <summary>
    /// RSI state for the vending machine's eject animation.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
    /// </summary>
    [DataField("ejectState")]
    public string? EjectState;

    /// <summary>
    /// RSI state for the vending machine's deny animation. Will either be played once as sprite flick
    /// or looped depending on how <see cref="LoopDenyAnimation"/> is set.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
    /// </summary>
    [DataField("denyState")]
    public string? DenyState;

    /// <summary>
    /// RSI state for when the vending machine is unpowered.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
    /// </summary>
    [DataField("brokenState")]
    public string? BrokenState;

    /// <summary>
    /// If set to <c>true</c> (default) will loop the animation of the <see cref="DenyState"/> for the duration
    /// of <see cref="SharedVendingMachineComponent.DenyDelay"/>. If set to <c>false</c> will play a sprite
    /// flick animation for the state and then linger on the final frame until the end of the delay.
    /// </summary>
    [DataField("loopDeny")]
    public bool LoopDenyAnimation = true;
}
