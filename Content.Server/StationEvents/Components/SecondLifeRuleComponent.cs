using Content.Server.StationEvents.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Ghost.Roles;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(SecondLifeRule))]
public sealed class SecondLifeRuleComponent : Component
{
    /// <summary>
    ///     Roles granted by this event along with the player limit on each (0 for no limit)
    /// </summary>
    [DataField("roles", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, JobPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, int> Roles = new();

    [DataField("announce")]
    public string Announce = "second-life-announce";

    [DataField("announceDead")]
    public string AnnounceDead = "second-life-announce-dead";

    [DataField("description")]
    public string Description = "Unknown";

    [DataField("rules")]
    public string Rules = "ghost-role-component-default-rules";

    [ViewVariables(VVAccess.ReadOnly)] public Dictionary<string, GhostRoleInfo> RoleInfo = new();
    [ViewVariables(VVAccess.ReadOnly)] public Dictionary<string, int> AcceptedCount = new();
    [ViewVariables(VVAccess.ReadOnly)] public List<IPlayerSession> PlayersSpawned = new();
}
