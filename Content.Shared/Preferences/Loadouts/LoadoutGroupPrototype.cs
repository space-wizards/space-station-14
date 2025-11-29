using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Corresponds to a set of loadouts for a particular slot.
/// </summary>
[Prototype]
public sealed partial class LoadoutGroupPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <inheritdoc />
    [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<LoadoutGroupPrototype>))]
    public string[]? Parents { get; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

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
    /// Number of loadouts that are selected by default.
    /// </summary>
    [DataField]
    public int DefaultSelected = 0;

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

    /// <summary>
    /// Individual loadout items part of this group
    /// </summary>
    [AlwaysPushInheritance]
    [DataField(required: true)]
    public List<ProtoId<LoadoutPrototype>> Loadouts = new();

    /// <summary>
    /// Child loadout groups to be displayed in the form of collapsible menu items
    /// </summary>
    [DataField]
    public List<ProtoId<LoadoutGroupPrototype>> LoadoutGroups = new();

    /// <summary>
    /// When this loadout group is used as child group of another group, this item will be used to represent it
    /// </summary>
    [DataField]
    public ProtoId<LoadoutPrototype>? DisplayLoadout;

    [DataField]
    public LocId? Description;

    /// <summary>
    /// When this loadout group is used as child group of another group, this entity's sprite will be used to represent it. If null, will fall back to the first item in the group
    /// </summary>
    [DataField]
    public EntProtoId? DummyEntity;
}
