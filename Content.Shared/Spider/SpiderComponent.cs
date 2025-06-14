using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Spider;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed partial class SpiderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("webPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WebPrototype = "SpiderWeb";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("webAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WebAction = "ActionSpiderWeb";

    [DataField] public EntityUid? Action;

    /// <summary>
    /// Whether the spider will spawn webs when not controlled by a player.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool SpawnsWebsAsNonPlayer = true;

    /// <summary>
    /// The cooldown in seconds between web spawns when not controlled by a player.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WebSpawnCooldownSeconds = 120f;

    /// <summary>
    /// The next time the spider can spawn a web when not controlled by a player.
    /// </summary>
    [DataField, ViewVariables]
    public TimeSpan NextWebSpawn = TimeSpan.Zero;
}

public sealed partial class SpiderWebActionEvent : InstantActionEvent { }
