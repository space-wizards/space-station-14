using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

[Prototype("preSpawnGearOverride")]
public sealed partial class PreSpawnGearOverridePrototype : IPrototype
    {
    /// <summary>
    /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
    /// </summary>
    [DataField]
    public StartingGearOverrideType? OverrideType { get; private set; } = null;

    /// <summary>
    /// if empty, there is no skirt override - instead the uniform provided in equipment is added.
    /// </summary>
    [DataField]
    public StartingGearPrototype? GearTemplate { get; private set; } = null;

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

}
