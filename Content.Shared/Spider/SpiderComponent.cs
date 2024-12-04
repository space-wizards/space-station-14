using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Spider;

/// <summary>
/// Gives the entity (probably a spider) an ability to spawn webs around itself.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed partial class SpiderComponent : Component
{
    /// <summary>
    /// Id of the entity getting spawned.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WebPrototype = "SpiderWeb";

    /// <summary>
    /// Id of the action that will be given.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnWebAction = "ActionSpiderWeb";

    /// <summary>
    /// Action given to the player.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActionEntity;

    /// <summary>
    /// List of entities proto will spawn on.
    /// </summary>
    [DataField]
    public EntityWhitelist? DestinationWhitelist;

    /// <summary>
    /// List of entities proto won't spawn on.
    /// </summary>
    [DataField]
    public EntityWhitelist? DestinationBlacklist;

    /// Popup text
    [DataField("inSpace")] public string _offGrid = "spider-web-action-nogrid";
    [DataField("success")] public string _success = "spider-web-action-success";
    [DataField("failure")] public string _fail = "spider-web-action-fail";
}

public sealed partial class SpiderWebActionEvent : InstantActionEvent
{
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
}
