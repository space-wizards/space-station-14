using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Corresponds to a Job / Antag prototype and specifies loadouts
/// </summary>
[Prototype("roleLoadout")]
public sealed class RoleLoadoutPrototype : IPrototype
{
    /*
     * Separate to JobPrototype / AntagPrototype as they are turning into messy god classes.
     */

    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// Groups that comprise this role loadout.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<LoadoutGroupPrototype>> Groups = new();
}
