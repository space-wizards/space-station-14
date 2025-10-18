using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

/// <summary>
/// A chameleon clothing outfit. Used for the chameleon controller jobs! Has various fields to help describe a full
/// job - all the fields are optional and override each other if necessary so you should fill out the maximum amount
/// that make sense for the best outcome.
/// </summary>
[Prototype]
public sealed partial class ChameleonOutfitPrototype : IPrototype
{
    /// <inheritdoc/>
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Job this outfit is based off of. Will use various things (job icon, job name, loadout etc...) for the outfit.
    /// This has the lowest priority for clothing if the user has no custom loadout, but highest if they do.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// Name of the outfit. This will be used for varous things like the chameleon controller UI and the agent IDs job
    /// name.
    /// </summary>
    [DataField]
    public LocId? Name;

    /// <summary>
    /// This name is only used in the chameleon controller UI.
    /// </summary>
    [DataField]
    public LocId? LoadoutName;

    /// <summary>
    /// Generic staring gear. Sometimes outfits don't have jobs but do have starting gear (E.g. Cluwne).
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    /// <summary>
    /// Icon for the outfit - used for stuff like the UI or agent ID.
    /// </summary>
    [DataField]
    public ProtoId<JobIconPrototype>? Icon;

    [DataField]
    public List<ProtoId<DepartmentPrototype>>? Departments;

    [DataField]
    public bool HasMindShield;

    /// <summary>
    /// Custom equipment for this specific chameleon outfit. If your making a new outfit that's just for the controller
    /// use this! It can be mixed with the rest of the fields though, it just takes highest priority right under
    /// user specified loadouts.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();
}
