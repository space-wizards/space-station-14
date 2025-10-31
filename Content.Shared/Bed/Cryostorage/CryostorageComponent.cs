using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Bed.Cryostorage;

/// <summary>
/// This is used for a container which, when a player logs out while inside of,
/// will delete their body and redistribute their items.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CryostorageComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ContainerId = "storage";

    /// <summary>
    /// How long a player can remain inside Cryostorage before automatically being taken care of, given that they have no mind.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan NoMindGracePeriod = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// How long a player can remain inside Cryostorage before automatically being taken care of.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan GracePeriod = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// A list of players who have actively entered cryostorage.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public List<EntityUid> StoredPlayers = new();

    /// <summary>
    /// Sound that is played when a player is removed by a cryostorage.
    /// </summary>
    [DataField]
    public SoundSpecifier? RemoveSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}

[Serializable, NetSerializable]
public enum CryostorageVisuals : byte
{
    Full
}

[Serializable, NetSerializable]
public record struct CryostorageContainedPlayerData()
{
    /// <summary>
    /// The player's IC name
    /// </summary>
    public string PlayerName = string.Empty;

    /// <summary>
    /// The player's entity
    /// </summary>
    public NetEntity PlayerEnt = NetEntity.Invalid;

    /// <summary>
    /// A dictionary relating a slot definition name to the name of the item inside of it.
    /// </summary>
    public Dictionary<string, string> ItemSlots = new();

    /// <summary>
    /// A dictionary relating a hand ID to the hand name and the name of the item being held.
    /// </summary>
    public Dictionary<string, string> HeldItems = new();
}

[Serializable, NetSerializable]
public sealed class CryostorageBuiState : BoundUserInterfaceState
{
    public List<CryostorageContainedPlayerData> PlayerData;

    public CryostorageBuiState(List<CryostorageContainedPlayerData> playerData)
    {
        PlayerData = playerData;
    }
}

[Serializable, NetSerializable]
public sealed class CryostorageRemoveItemBuiMessage : BoundUserInterfaceMessage
{
    public NetEntity StoredEntity;

    public string Key;

    public RemovalType Type;

    public enum RemovalType : byte
    {
        Hand,
        Inventory
    }

    public CryostorageRemoveItemBuiMessage(NetEntity storedEntity, string key, RemovalType type)
    {
        StoredEntity = storedEntity;
        Key = key;
        Type = type;
    }
}

[Serializable, NetSerializable]
public enum CryostorageUIKey : byte
{
    Key
}
