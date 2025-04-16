using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Cloning;

/// <summary>
///     Settings for cloning a humanoid.
///     Used to decide which components should be copied.
/// </summary>
[Prototype]
public sealed partial class CloningSettingsPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(PrototypeIdArraySerializer<CloningSettingsPrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; }

    /// <summary>
    ///     Determines if cloning can be prevented by traits etc.
    /// </summary>
    [DataField]
    public bool ForceCloning = true;

    /// <summary>
    ///     Which inventory slots will receive a copy of the original's clothing.
    ///     Disabled when null.
    /// </summary>
    [DataField]
    public SlotFlags? CopyEquipment = SlotFlags.All;

    /// <summary>
    ///     Whether or not to copy slime storage and storage implant contents.
    /// </summary>
    [DataField]
    public bool CopyInternalStorage = true;

    /// <summary>
    ///     Whether or not to copy implants.
    /// </summary>
    [DataField]
    public bool CopyImplants = true;

    /// <summary>
    ///     Whitelist for the equipment allowed to be copied.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     Blacklist for the equipment allowed to be copied.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// TODO: Make this not a string https://github.com/space-wizards/RobustToolbox/issues/5709
    /// <summary>
    ///     Components to copy from the original to the clone.
    ///     This only makes a shallow copy of datafields!
    ///     If you need a deep copy or additional component initialization, then subscribe to CloningEvent instead!
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> Components = new();
}
