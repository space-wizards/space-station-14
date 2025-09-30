using Robust.Shared.Localization;

namespace Content.Shared.Access;

public static class AccessGroupPrototypeExtensions
{
    /// <summary>
    /// Gets the localized name of an access group prototype.
    /// </summary>
    public static string GetAccessGroupName(this AccessGroupPrototype prototype)
    {
        return Loc.GetString($"access-group-{prototype.ID}");
    }
}
