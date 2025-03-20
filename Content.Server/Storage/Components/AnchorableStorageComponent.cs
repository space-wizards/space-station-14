using Content.Shared.Whitelist;

namespace Content.Server.Storage;


[RegisterComponent]
public sealed partial class AnchorableStorageComponent : Component
{
    /// <summary>
    ///     Blacklist for what items can be stored in the stash when anchored.
    ///     Will eject all entities that match the blacklist when anchored.
    /// </summary>
    [DataField]
    public EntityWhitelist? StorageBlacklist;
}
