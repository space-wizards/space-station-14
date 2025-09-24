using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.WebPlacer;

/// <summary>
/// Gives the entity (probably a spider) an action to spawn entities (probably webs) around itself.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedWebPlacerSystem))]
public sealed partial class WebPlacerComponent : Component
{
    /// <summary>
    /// Id of the entity getting spawned.
    /// </summary>
    [DataField("spawn")]
    public EntProtoId SpawnId = "SpiderWeb";

    /// <summary>
    /// Id of the action that will be given.
    /// </summary>
    [DataField]
    public EntProtoId SpawnWebAction = "ActionSpiderWeb";

    /// <summary>
    /// Actual action given to the player.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Whitelist of entities to only spawn on.
    /// </summary>
    [DataField]
    public EntityWhitelist? DestinationWhitelist;

    /// <summary>
    /// Blacklist of entities to never spawn on.
    /// </summary>
    [DataField]
    public EntityWhitelist? DestinationBlacklist;

    /// <summary>
    /// Sound played when successfully spawning something.
    /// </summary>
    [DataField]
    public SoundSpecifier? WebSound =
            new SoundPathSpecifier("/Audio/Effects/spray3.ogg")
            {
                Params = AudioParams.Default.WithVariation(0.125f),
            };

    /// <summary>
    /// Vectors determining where the entities will spawn.
    /// </summary>
    [DataField]
    public List<Vector2i> OffsetVectors = new()
    {
        Vector2i.Zero,
        Vector2i.Up,
        Vector2i.Down,
        Vector2i.Left,
        Vector2i.Right,
    };

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

    /// <summary>
    /// Webs cannot be placed because the component owner is not on a valid grid (e.g. in space).
    /// </summary>
    [DataField]
    public LocId MessageOffGrid = "spider-web-action-off-grid";

    /// <summary>
    /// At least one web was placed.
    /// </summary>
    [DataField]
    public LocId MessageSuccess = "spider-web-action-success";

    /// <summary>
    /// Webs failed to be placed (e.g. no valid spawn destination).
    /// </summary>
    [DataField]
    public LocId MessageNoSpawn = "spider-web-action-no-spawn";
}

public sealed partial class SpiderWebActionEvent : InstantActionEvent
{
}
