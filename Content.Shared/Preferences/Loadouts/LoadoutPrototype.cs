using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Individual loadout item to be applied.
/// </summary>
[Prototype]
public sealed partial class LoadoutPrototype : IPrototype, IEquipmentLoadout
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /*
     * You can either use an existing StartingGearPrototype or specify it inline to avoid bloating yaml.
     */

    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    /// <summary>
    /// Effects to be applied when the loadout is applied.
    /// These can also return true or false for validation purposes.
    /// </summary>
    [DataField]
    public List<LoadoutEffect> Effects = new();

    /// <inheritdoc />
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    public List<EntProtoId> Inhand { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();
}
