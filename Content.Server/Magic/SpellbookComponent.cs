using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Magic;

[RegisterComponent]
[Friend(typeof(SpellSystem))]
public sealed partial class SpellbookComponent : Component
{
    [ViewVariables]
    public readonly List<ActionType> Spells = new();

    // Dictionaries of spell prototypes. dictionary values are the number of charges available, which override the
    // prototype defaults. Negative numbers mean no limit.

    [DataField("worldSpells", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<int, WorldTargetActionPrototype>))]
    public readonly Dictionary<string, int> WorldSpells = new();

    [DataField("entitySpells", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, EntityTargetActionPrototype>))]
    public readonly Dictionary<string, int> EntitySpells = new();

    [DataField("instantSpells", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, InstantActionPrototype>))]
    public readonly Dictionary<string, int> InstantSpells = new();
}
