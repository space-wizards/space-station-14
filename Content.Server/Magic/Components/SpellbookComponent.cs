using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Magic.Components;

/// <summary>
/// Spellbooks for having an entity learn spells as long as they've read the book and it's in their hand.
/// </summary>
[RegisterComponent]
public sealed class SpellbookComponent : Component
{
    /// <summary>
    /// List of spells that this book has. This is a combination of the WorldSpells, EntitySpells, and InstantSpells.
    /// </summary>
    [ViewVariables]
    public readonly List<ActionType> Spells = new();

    /// <summary>
    /// The three fields below is just used for initialization.
    /// </summary>
    [DataField("worldSpells", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, WorldTargetActionPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly Dictionary<string, int> WorldSpells = new();

    [DataField("entitySpells", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, EntityTargetActionPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly Dictionary<string, int> EntitySpells = new();

    [DataField("instantSpells", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, InstantActionPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public readonly Dictionary<string, int> InstantSpells = new();

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
