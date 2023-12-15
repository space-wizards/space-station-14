using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Magic.Components;

/// <summary>
/// Spellbooks for having an entity learn spells as long as they've read the book and it's in their hand.
/// </summary>
[RegisterComponent]
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
    [DataField("spells", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, EntityPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, int> SpellActions = new();

    [DataField("learnTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float LearnTime = .75f;

    /// <summary>
    ///  If true, the spell action stays even after the book is removed
    /// </summary>
    [DataField("learnPermanently")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LearnPermanently;
}
