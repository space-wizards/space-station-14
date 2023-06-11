// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CryopodSSD;

[RegisterComponent]
public sealed class SSDStorageConsoleComponent : Component
{
    /// <summary>
    /// List for IC knowing who went in cryo
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> StoredEntities = new List<string>();
    
    /// <summary>
    /// All items that are not whitelisted will be
    /// irretrievably lost after the essence is transferred to cryostorage.
    /// </summary>
    [DataField("whitelist"), ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist = null;

    /// <summary>
    /// Radius to check if the console can handle the TransferredEntity event
    /// of the cryopods
    /// </summary>
    [DataField("radiusToConnect"), ViewVariables(VVAccess.ReadWrite)]
    public float RadiusToConnect = 500f;

    /// <summary>
    /// We want the cryopod to have ConsoleComponent for
    /// situations where there are no available console entity nearly
    /// that variable for knowing is it really console or cryopod
    /// </summary>
    [DataField("isItReallyCryopod"), ViewVariables(VVAccess.ReadOnly)]
    public bool IsCryopod = false;
}

[Serializable, NetSerializable]
public sealed class CryopodSSDStorageInteractWithItemEvent : BoundUserInterfaceMessage
{
    public readonly EntityUid InteractedItemUid;
    public CryopodSSDStorageInteractWithItemEvent(EntityUid interactedItemUid)
    {
        InteractedItemUid = interactedItemUid;
    }
}