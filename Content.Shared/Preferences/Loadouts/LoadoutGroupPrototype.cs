// Modified by Ronstation contributor(s), therefore this file is licensed as MIT sublicensed with AGPL-v3.0.
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array; // Ronstation - modification.

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Corresponds to a set of loadouts for a particular slot.
/// </summary>
[Prototype]
public sealed partial class LoadoutGroupPrototype : IPrototype, IInheritingPrototype // Ronstation - modification
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// User-friendly name for the group.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Minimum number of loadouts that need to be specified for this category.
    /// </summary>
    [DataField]
    public int MinLimit = 1;

    /// <summary>
    /// Maximum limit for the category.
    /// </summary>
    [DataField]
    public int MaxLimit = 1;

    /// <summary>
    /// Hides the loadout group from the player.
    /// </summary>
    [DataField]
    public bool Hidden;

    // Ronstation - start of modifications.

    /// <summary>
    /// Minimum number of loadouts that need to be picked by default
    /// </summary>
    [DataField]
    public int DefaultMinLimit = 0;
    
    /// <summary>
    /// The prototype we inherit from.
    /// </summary>
    [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<LoadoutGroupPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }
    // Ronstation - end of modifications.

    [DataField(required: true)]
    [AlwaysPushInheritance] // Ronstation - modification.
    public List<ProtoId<LoadoutPrototype>> Loadouts = new();
}
