using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.SmartFridge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SmartFridgeComponent : Component
{
    [DataField]
    public string Container = "smart_fridge_inventory";

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [DataField, AutoNetworkedField]
    public List<SmartFridgeEntry> Entries = new();

    [DataField, AutoNetworkedField]
    public Dictionary<SmartFridgeEntry, List<NetEntity>> ContainedEntries = new();

    /// <summary>
    ///     Sound that plays when ejecting an item
    /// </summary>
    [DataField]
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
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}

[Serializable, NetSerializable, DataRecord]
public record struct SmartFridgeEntry(string Name)
{
    public string Name = Name;
}

[Serializable, NetSerializable]
public enum SmartFridgeUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SmartFridgeDispenseItemMessage(SmartFridgeEntry entry) : BoundUserInterfaceMessage
{
    public SmartFridgeEntry Entry = entry;
}
