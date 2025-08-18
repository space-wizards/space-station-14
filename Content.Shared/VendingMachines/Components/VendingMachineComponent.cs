using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.VendingMachines.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class VendingMachineComponent : Component
{
    /// <summary>
    /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
    /// </summary>
    [DataField("pack", required: true)]
    public ProtoId<VendingMachineInventoryPrototype> PackPrototypeId;

    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Deny" state.
    /// Used by the client to determine how long the deny animation should be played.
    /// </summary>
    [DataField]
    public TimeSpan DenyDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Eject" state.
    /// The selected item is dispensed afer this delay.
    /// Used by the client to determine how long the deny animation should be played.
    /// </summary>
    [DataField]
    public TimeSpan EjectDelay = TimeSpan.FromSeconds(1.2);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> Inventory = [];

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = [];

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = [];

    [ViewVariables]
    public bool Ejecting => EjectEnd != null;

    [ViewVariables]
    public bool Denying => DenyEnd != null;

    public string? NextItemToEject;

    /// <summary>
    ///
    /// </summary>
    public bool ThrowNextItem = false;

    /// <summary>
    ///     Sound that plays when ejecting an item
    /// </summary>
    [DataField]
    // Grabbed from: https://github.com/tgstation/tgstation/blob/d34047a5ae911735e35cd44a210953c9563caa22/sound/machines/machine_vend.ogg
    public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg")
    {
        Params = new AudioParams
        {
            Volume = -4f,
            Variation = 0.15f
        }
    };

    /// <summary>
    ///     Sound that plays when an item can't be ejected
    /// </summary>
    [DataField]
    // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public float NonLimitedEjectForce = 7.5f;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public float NonLimitedEjectRange = 5f;

    /// <summary>
    /// The quality of the stock in the vending machine on spawn.
    /// Represents the percentage chance (0.0f = 0%, 1.0f = 100%) each set of items in the machine is fully-stocked.
    /// If not fully stocked, the stock will have a random value between 0 (inclusive) and max stock (exclusive).
    /// </summary>
    [DataField]
    public float InitialStockQuality = 1.0f;

    /// <summary>
    ///
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? EjectEnd;

    /// <summary>
    ///
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? DenyEnd;

    /// <summary>
    ///     While disabled by EMP it randomly ejects items
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEmpEject = TimeSpan.Zero;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VendingMachineInventoryEntry(InventoryType type, string id, uint amount)
{
    [DataField]
    public InventoryType Type = type;

    [DataField]
    public string ID = id;

    [DataField]
    public uint Amount = amount;
}

[Serializable, NetSerializable]
public enum InventoryType : byte
{
    Regular,
    Emagged,
    Contraband
}

[Serializable, NetSerializable]
public enum VendingMachineVisuals : byte
{
    VisualState
}

[Serializable, NetSerializable]
public enum VendingMachineVisualState : byte
{
    Normal,
    Off,
    Broken,
    Eject,
    Deny,
}

[Serializable, NetSerializable]
public enum ContrabandWireKey : byte
{
    StatusKey,
    TimeoutKey
}

[Serializable, NetSerializable]
public enum EjectWireKey : byte
{
    StatusKey,
}

public sealed partial class VendingMachineSelfDispenseEvent : InstantActionEvent;
