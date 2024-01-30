using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

[Prototype("preSpawnGearOverride")]
public sealed partial class SpawnGearOverridePrototype : IPrototype
    {
    /// <summary>
    /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
    /// </summary>
    [DataField]
    public SpawnGearOverrideType? OverrideType { get; private set; } = null;

    /// <summary>
    /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
    /// </summary>
    [DataField]
    public StartingGearPrototype? GearTemplate { get; private set; } = null;

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = string.Empty;
}

public enum SpawnGearOverrideType : byte
{
    /// <summary>
    /// Adds the item to the slot if the targeted slot is not getting job-specified gear
    /// </summary>
    Add,
    /// <summary>
    /// Adds the item to the slot, overriding job-specified gear
    /// </summary>
    Force,
    /// <summary>
    /// Removes job-specified items from every slot where input is specified (no matter what the input entity is), leaving the slot empty
    /// For removing Backpacks or Jumpsuits, you must also specify removal for DuffelBag, Satchel and InnerClothingSkirt, outside the Equipment list
    /// </summary>
    Remove
}
