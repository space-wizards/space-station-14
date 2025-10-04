using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SmartFridge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SmartFridgeSystem))]
public sealed partial class SmartFridgeComponent : Component
{
    /// <summary>
    /// The container ID that this SmartFridge stores its inventory in
    /// </summary>
    [DataField]
    public string Container = "smart_fridge_inventory";

    /// <summary>
    /// Whitelist for what entities can be inserted
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for what entities can be inserted
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The sound played on inserting an item into the fridge
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound = new SoundCollectionSpecifier("MachineInsert");

    /// <summary>
    /// A list of entries to display in the UI
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<SmartFridgeEntry> Entries = new();
    // Consider replacing with a sorted dictionary?
    // Then entries don't have to be networked & serialized twice here & in ContainedEntries

    /// <summary>
    /// A mapping of smart fridge entries to the actual contained contents
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(SmartFridgeSystem), Other = AccessPermissions.ReadExecute)]
    public Dictionary<SmartFridgeEntry, HashSet<NetEntity>> ContainedEntries = new();

    /// <summary>
    /// The flavour text displayed at the bottom of the SmartFridge's UI
    /// </summary>
    [DataField]
    public LocId FlavorText = "smart-fridge-request-generic";

    /// <summary>
    /// Sound that plays when ejecting an item
    /// </summary>
    [DataField]
    public SoundSpecifier SoundVend = new SoundCollectionSpecifier("VendingDispense")
    {
        Params = new AudioParams
        {
            Volume = -4f,
            Variation = 0.15f
        }
    };

    /// <summary>
    /// Sound that plays when an item can't be ejected
    /// </summary>
    [DataField]
    public SoundSpecifier SoundDeny = new SoundCollectionSpecifier("VendingDeny");
}

[Serializable, NetSerializable]
public record struct SmartFridgeEntry(string Name)
{
    // Warning: if you add more fields to this, you need to update SmartFridgeEntrySerializer
    // This cannot simply use a DataRecord attribute, as this struct is used as a yaml dictionary key, so it must
    // serialize to a ValueDataNode
    public string Name = Name;
}

[Serializable, NetSerializable]
public enum SmartFridgeUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SmartFridgeDispenseItemMessage(SmartFridgeEntry entry) : BoundUserInterfaceMessage
{
    public SmartFridgeEntry Entry = entry;
}
