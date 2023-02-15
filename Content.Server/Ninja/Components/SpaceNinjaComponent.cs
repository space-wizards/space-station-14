using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed class SpaceNinjaComponent : Component
{
    /// <summary>
    /// The role prototype of the space ninja antag role
    /// </summary>
    [DataField("ninjaRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public readonly string SpaceNinjaRoleId = "SpaceNinja";

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
