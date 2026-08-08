using Content.Client.UserInterface.RichText;
using Content.Client.DeadSpace.UserInterface.RichText;
using Robust.Client.UserInterface.RichText;

namespace Content.Client.RichText;

/// <summary>
/// Contains rules for what markup tags are allowed to be used by players.
/// </summary>
public static class UserFormattableTags
{
    /// <summary>
    /// The basic set of "rich text" formatting tags that shouldn't cause any issues.
    /// Limit user rich text to these by default.
    /// </summary>
    public static readonly Type[] BaseAllowedTags =
    [
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(HeadingTag),
        typeof(ItalicTag),
        typeof(MonoTag),
        // DS14-start
        typeof(ConfusionTag),
        typeof(CyrillicConfusionTag),
        typeof(CutTag),
        typeof(ShiftTag),
        typeof(SmallTag),
        typeof(UnderlineTag),
        // DS14-end
    ];
}
