using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.WebPlacer;

/// <summary>
/// Gives the entity (probably a spider) an ability to spawn entites (probably webs) around itself.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedWebPlacerSystem))]
public sealed partial class WebPlacerComponent : Component
{
    /// <summary>
    /// Id of the entity getting spawned.
    /// </summary>
    [DataField]
    public EntProtoId WebPrototype = "SpiderWeb";

    /// <summary>
    /// Id of the action that will be given.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public EntProtoId SpawnWebAction = "ActionSpiderWeb";

    /// <summary>
    /// Action given to the player.
    /// </summary>
    //[ViewVariables]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Whitelist of entities proto will only spawn on.
    /// </summary>
    [DataField]
    public EntityWhitelist? DestinationWhitelist;

    /// <summary>
    /// Blacklist of entities proto won't spawn on.
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

    // Localization files for popup text.
    [DataField]
    public LocId MessageOffGrid = "spider-web-action-nogrid";
    [DataField]
    public LocId MessageSuccess = "spider-web-action-success";
    [DataField]
    public LocId MessageFail = "spider-web-action-fail";
}

public sealed partial class SpiderWebActionEvent : InstantActionEvent
{
}
