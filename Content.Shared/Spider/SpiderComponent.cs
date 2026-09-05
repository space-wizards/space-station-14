using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Spider;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed partial class SpiderComponent : Component
{
    [DataField]
    public EntProtoId WebPrototype = "SpiderWeb";

    [DataField]
    public EntProtoId WebAction = "ActionSpiderWeb";

    [DataField] public EntityUid? Action;

    /// <summary>
    /// Whether the spider will spawn webs when not controlled by a player.
    /// </summary>
    [DataField]
    public bool SpawnsWebsAsNonPlayer = true;

    /// <summary>
    /// The cooldown in seconds between web spawns when not controlled by a player.
    /// </summary>
    [DataField]
    public TimeSpan WebSpawnCooldown = TimeSpan.FromSeconds(45f);

    /// <summary>
    /// The next time the spider can spawn a web when not controlled by a player.
    /// </summary>
    [DataField]
    public TimeSpan? NextWebSpawn;
}

public sealed partial class SpiderWebActionEvent : InstantActionEvent { }
