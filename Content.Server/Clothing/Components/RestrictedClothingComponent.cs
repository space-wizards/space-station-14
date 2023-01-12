using Content.Server.Speech.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Clothing.Components;

/// <summary>
///     Does bad things when someone tries to wear something they shouldn't
/// </summary>
[RegisterComponent]
public sealed class RestrictedClothingComponent : Component
{
    /// <summary>
    ///     List of permissions required to be allowed to wear the item
    /// </summary>
    [DataField("permissions")] [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> Permissions = new();

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
