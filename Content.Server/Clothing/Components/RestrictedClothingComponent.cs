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
    [DataField("permissions")]
    public HashSet<string> Permissions = new();

    /// <summary>
    ///     Uid that won't be affected
    /// </summary>
    // [DataField("whitelistedUid")]
    // public int? WhitelistedUid;

    /// <summary>
    ///     Always checks the whitelist
    /// </summary>
    [DataField("requireWhitelist")]
    public bool RequireWhitelist;
}
