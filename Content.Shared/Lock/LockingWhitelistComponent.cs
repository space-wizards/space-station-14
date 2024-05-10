using Content.Shared.Whitelist;

namespace Content.Shared.Lock;

/// <summary>
/// Adds whitelist and blacklist for this mob to lock things.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LockingWhitelistSystem))]
public sealed partial class LockingWhitelistComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
