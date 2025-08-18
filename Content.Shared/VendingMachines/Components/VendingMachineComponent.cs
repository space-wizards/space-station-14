using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class VendingMachineComponent : Component
{
    /// <summary>
    /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
    /// </summary>
    // Okay so not using ProtoId here is load-bearing because the ProtoId serializer will log errors if the prototype doesn't exist.
    [DataField("pack", required: true)]
    public ProtoId<VendingMachineInventoryPrototype> PackPrototypeId = string.Empty;

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

    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> Inventory = [];

    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = [];

    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = [];

    /// <summary>
    /// If true then unlocks the <see cref="ContrabandInventory"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Contraband;

    [ViewVariables]
    public bool Ejecting => EjectEnd != null;

    [ViewVariables]
    public bool Denying => DenyEnd != null;

    [ViewVariables]
    public bool DispenseOnHitCoolingDown => DispenseOnHitEnd != null;

    [DataField]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? EjectEnd;

    [DataField]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? DenyEnd;

    [DataField]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan? DispenseOnHitEnd;

    public string? NextItemToEject;

    public bool Broken;

    /// <summary>
    /// When true, will forcefully throw any object it dispenses
    /// </summary>
    [DataField]
    public bool CanShoot = false;

    public bool ThrowNextItem = false;

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
    ///     This value is separate to <see cref="VendingMachineComponent.EjectDelay"/> because that value might be
    ///     0 for a vending machine for legitimate reasons (no desired delay/no eject animation)
    ///     and can be circumvented with forced ejections.
    /// </summary>
    [DataField]
    public TimeSpan? DispenseOnHitCooldown = TimeSpan.FromSeconds(1.0);

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

    public float NonLimitedEjectForce = 7.5f;

    public float NonLimitedEjectRange = 5f;

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
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VendingMachineInventoryEntry
{
    [DataField]
    public InventoryType Type;

    [DataField]
    public string ID;

    [DataField]
    public uint Amount;

    public VendingMachineInventoryEntry(InventoryType type, string id, uint amount)
    {
        Type = type;
        ID = id;
        Amount = amount;
    }

    public VendingMachineInventoryEntry(VendingMachineInventoryEntry entry)
    {
        Type = entry.Type;
        ID = entry.ID;
        Amount = entry.Amount;
    }
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

public enum VendingMachineVisualLayers : byte
{
    /// <summary>
    /// Off / Broken. The other layers will overlay this if the machine is on.
    /// </summary>
    Base,
    /// <summary>
    /// Normal / Deny / Eject
    /// </summary>
    BaseUnshaded,
    /// <summary>
    /// Screens that are persistent (where the machine is not off or broken)
    /// </summary>
    Screen
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
