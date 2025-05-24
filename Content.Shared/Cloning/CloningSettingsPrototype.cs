using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

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
    ///     Components to copy from the original to the clone using CopyComp.
    ///     This makes a deepcopy of all datafields, including information the clone might not own!
    ///     If you need to exclude data or do additional component initialization, then subscribe to CloningEvent instead!
    ///     Components in this list that the orginal does not have will be removed from the clone.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> Components = new();

    /// <summary>
    ///     Components to remove from the clone and copy over manually using a CloneEvent raised on the original.
    ///     Use this when the component cannot be copied using CopyComp, for example when having an Uid as a datafield.
    ///</summary>
    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> EventComponents = new();
}
