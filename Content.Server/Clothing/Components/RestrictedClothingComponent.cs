using Content.Server.Clothing.Systems;
using Content.Shared.Administration;

namespace Content.Server.Clothing.Components;

/// <summary>
///     Does bad things when someone tries to wear something they shouldn't
/// </summary>
[RegisterComponent]
[Access(typeof(RestrictedClothingSystem))]
public sealed class RestrictedClothingComponent : Component
{
    /// <summary>
    ///     List of permissions required to be allowed to wear the item
    /// </summary>
    [DataField("permissions")] [ViewVariables(VVAccess.ReadWrite)]
    public AdminFlags Permissions = AdminFlags.None;

    /// <summary>
    ///     Uid that won't be affected
    /// </summary>
    [DataField("whitelistedUid")] [ViewVariables(VVAccess.ReadWrite)]
    public int WhitelistedUid = 0;

    /// <summary>
    ///     Always checks the whitelist
    /// </summary>
    [DataField("requireWhitelist")] [ViewVariables(VVAccess.ReadWrite)]
    public bool RequireWhitelist;
}
