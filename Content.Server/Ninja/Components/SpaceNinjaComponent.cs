using Content.Server.Objectives;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

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
    [DataField("objectives", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<ObjectivePrototype>))]
    public readonly HashSet<string> Objectives = new();

    /// <summary>
    /// List of implants to inject on spawn
    /// </summary>
    [DataField("implants", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<EntityPrototype>))]
    public readonly HashSet<string> Implants = new();

    /// Currently worn suit
    [DataField("suit")]
    public EntityUid? Suit = null;

    /// Number of doors that have been doorjacked, used for objective
    [ViewVariables]
    public int DoorsJacked = 0;

    /// Research nodes that have been downloaded, used for objective
    [ViewVariables]
    public HashSet<string> DownloadedNodes = new();
}
