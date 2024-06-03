using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Components;

/// <summary>
/// Spellbooks can grant one or more spells to the user. If marked as <see cref="LearnPermanently"/> it will teach
/// the performer the spells and wipe the book.
/// Default behavior requires the book to be held in hand
/// </summary>
[RegisterComponent, Access(typeof(SpellbookSystem))]
public sealed partial class SpellbookComponent : Component
{
    /// <summary>
    /// List of spells that this book has. This is a combination of the WorldSpells, EntitySpells, and InstantSpells.
    /// </summary>
    [ViewVariables]
    public readonly List<EntityUid> Spells = new();

    /// <summary>
    /// The three fields below is just used for initialization.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<EntProtoId, int> SpellActions = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float LearnTime = .75f;

    /// <summary>
    ///  If true, the spell action stays even after the book is removed
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LearnPermanently;
}
