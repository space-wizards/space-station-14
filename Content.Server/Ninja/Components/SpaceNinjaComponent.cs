using Content.Server.Objectives;
using Content.Shared.Ninja;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed class SpaceNinjaComponent : Component
{
    /// <summary>
    /// The role prototype of the space ninja antag role
    /// </summary>
    [DataField("ninjaRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public readonly string SpaceNinjaRoleId = "SpaceNinja";

    /// <summary>
    /// List of objective prototype ids to add
    /// </summary>
    [DataField("objectives", customTypeSerializer: typeof(PrototypeIdListSerializer<ObjectivePrototype>))]
    public readonly List<string> Objectives = new();

    /// <summary>
    /// List of implants to inject on spawn
    /// </summary>
    [DataField("implants", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public readonly List<string> Implants = new();

    /// <summary>
    /// List of threats that can be called in
    /// </summary>
    [DataField("threats")]
    public readonly List<Threat> Threats = new();

    /// Currently worn suit
    [ViewVariables]
    public EntityUid? Suit = null;

    /// Currently worn gloves
    [ViewVariables]
    public EntityUid? Gloves = null;

    /// Number of doors that have been doorjacked, used for objective
    [ViewVariables]
    public int DoorsJacked = 0;

    /// Research nodes that have been downloaded, used for objective
    [ViewVariables]
    public HashSet<string> DownloadedNodes = new();

    /// Warp point that the spider charge has to target
    [ViewVariables]
    public EntityUid? SpiderChargeTarget = null;

    /// Whether the spider charge has been detonated on the target, used for objective
    [ViewVariables]
    public bool SpiderChargeDetonated;

    /// Whether the comms console has been hacked, used for objective
    [ViewVariables]
    public bool CalledInThreat;
}

//[Serializable]
[DataDefinition]
public sealed class Threat
{
    [DataField("announcement")]
    public readonly string Announcement = default!;

    [DataField("rule")]
    public readonly string Rule = default!;
}
