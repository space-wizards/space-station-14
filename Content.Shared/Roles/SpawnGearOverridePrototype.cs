using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

[Prototype("spawnGearOverride")]
public sealed partial class SpawnGearOverridePrototype : IPrototype
    {
    /// <summary>
    /// This determines how the template will be applied to the starting gear.
    /// </summary>
    [DataField]
    public SpawnGearOverrideType? OverrideType { get; private set; } = null;

    /// <summary>
    /// This is the list of roles that will use this override. If null, the override will apply to any role.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>>? Role { get; private set; } = null;

    /// <summary>
    /// This is the gear template that will be applied.
    /// If you modify the Backpack or Jumpsuit, you should also modify DuffelBag and Satchel or InnerClothingSkirt, otherwise they will overwrite your change if the user has chosen those alternate options.
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
    /// Entries specified in GearTemplate will check if their target slot is empty, and be added only if they don't overwrite existing starting gear.
    /// </summary>
    Add,
    /// <summary>
    /// Entries specified in GearTemplate will be added to their target slots, overwriting existing items.
    /// </summary>
    Force,
    /// <summary>
    /// If an item, any item, is specified in a slot on GearTemplate then ANY item in the target slot will be deleted. I don't know why anyone would need this, but it exists now.
    /// </summary>
    Remove
}
