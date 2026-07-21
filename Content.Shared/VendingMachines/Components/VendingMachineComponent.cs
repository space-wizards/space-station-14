using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VendingMachineComponent : Component
{
    /// <summary>
    /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
    /// </summary>
    // Okay so not using ProtoId here is load-bearing because the ProtoId serializer will log errors if the prototype doesn't exist.
    [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>), required: true)]
    public string PackPrototypeId = string.Empty;

    [DataField]
    public Dictionary<string, VendingMachineInventoryEntry> Inventory = new();

    [DataField]
    public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = new();

    [DataField]
    public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = new();

    /// <summary>
    /// If true then unlocks the <see cref="ContrabandInventory"/>
    /// </summary>
    [DataField]
    public bool Contraband;

    [ViewVariables]
    public bool DispenseOnHitCoolingDown => DispenseOnHitEnd != null;

    [DataField]
    public TimeSpan? DispenseOnHitEnd;

    [DataField]
    public bool Broken;

    /// <summary>
    ///     The chance that a vending machine will randomly dispense an item on hit.
    ///     Chance is 0 if null.
    /// </summary>
    [DataField]
    public float? DispenseOnHitChance;

    /// <summary>
    ///     The minimum amount of damage that must be done per hit to have a chance
    ///     of dispensing an item.
    /// </summary>
    [DataField]
    public float? DispenseOnHitThreshold;

    /// <summary>
    ///     Amount of time in seconds that need to pass before damage can cause a vending machine to eject again.
    ///     This value is separate to <see cref="VendingMachineEjectComponent.EjectDelay"/> because that value might be
    ///     0 for a vending machine for legitimate reasons (no desired delay/no eject animation)
    ///     and can be circumvented with forced ejections.
    /// </summary>
    [DataField]
    public TimeSpan? DispenseOnHitCooldown = TimeSpan.FromSeconds(1.0);

    /// <summary>
    /// The quality of the stock in the vending machine on spawn.
    /// Represents the percentage chance (0.0f = 0%, 1.0f = 100%) each set of items in the machine is fully-stocked.
    /// If not fully stocked, the stock will have a random value between 0 (inclusive) and max stock (exclusive).
    /// </summary>
    [DataField]
    public float InitialStockQuality = 1.0f;

    /// <summary>
    ///     While disabled by EMP it randomly ejects items
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEmpEject = TimeSpan.Zero;

    /// <summary>
    /// Audio entity used during restock in case the doafter gets canceled.
    /// </summary>
    [DataField]
    public EntityUid? RestockStream;

    #region Client Visuals
    /// <summary>
    /// RSI state for when the vending machine is unpowered.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
    /// </summary>
    [DataField]
    public string? OffState;

    /// <summary>
    /// RSI state for the screen of the vending machine
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Screen"/>
    /// </summary>
    [DataField]
    public string? ScreenState;

    /// <summary>
    /// RSI state for the vending machine's normal state. Usually a looping animation.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
    /// </summary>
    [DataField]
    public string? NormalState;

    /// <summary>
    /// RSI state for the vending machine's eject animation.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
    /// </summary>
    [DataField]
    public string? EjectState;

    /// <summary>
    /// RSI state for the vending machine's deny animation. Will either be played once as sprite flick
    /// or looped depending on how <see cref="LoopDenyAnimation"/> is set.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.BaseUnshaded"/>
    /// </summary>
    [DataField]
    public string? DenyState;

    /// <summary>
    /// RSI state for when the vending machine is unpowered.
    /// Will be displayed on the layer <see cref="VendingMachineVisualLayers.Base"/>
    /// </summary>
    [DataField]
    public string? BrokenState;

    /// <summary>
    /// If set to <c>true</c> (default) will loop the animation of the <see cref="DenyState"/> for the duration
    /// of <see cref="VendingMachineEjectComponent.DenyDelay"/>. If set to <c>false</c> will play a sprite
    /// flick animation for the state and then linger on the final frame until the end of the delay.
    /// </summary>
    [DataField("loopDeny")]
    public bool LoopDenyAnimation = true;
    #endregion
}
