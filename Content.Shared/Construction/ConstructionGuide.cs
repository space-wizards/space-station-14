using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction;

/// <summary>
/// A set of instructions on how to construct (or deconstruct) something.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConstructionGuide(ConstructionGuideEntry[] entries)
{
    /// <summary>
    /// The set of entries (a step or a condition) describing how to build this.
    /// </summary>
    public readonly ConstructionGuideEntry[] Entries = entries;
}

/// <summary>
/// An individual entry in a set of instructions to build something.
/// May represent a condition ("the tile this is on must be empty"),
/// or an action ("add 2 steel sheets").
/// </summary>
[Serializable, NetSerializable]
public sealed class ConstructionGuideEntry
{
    /// <summary>
    /// The number to display with this entry.
    /// Normally 1-indexed.
    /// </summary>
    public int? EntryNumber { get; set; } = null;

    /// <summary>
    /// The number of spaces to prefix this text with.
    /// </summary>
    public int Padding { get; set; } = 0;

    /// <summary>
    /// The localization ID of the string to print out for this entry.
    /// </summary>
    public LocId Localization { get; set; } = string.Empty;

    /// <summary>
    /// A set of arbitrary key/value data relating to this entry.
    /// </summary>
    public (string, object)[]? Arguments { get; set; } = null;

    /// <summary>
    /// An optional sprite to represent this entry.
    /// </summary>
    public SpriteSpecifier? Icon { get; set; } = null;

    /// <summary>
    /// Returns true if this entry contains no actual information.
    /// </summary>
    public bool Empty()
    {
        return EntryNumber == null
            && Padding == 0
            && Localization == string.Empty
            && Arguments == null
            && Icon == null;
    }
}
