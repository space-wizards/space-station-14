using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Spider;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed partial class SpiderComponent : Component
{
    /// <summary>
    /// Id of the entity getting spawned.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true,
               customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WebPrototype;

    /// <summary>
    /// List of entities that prevent spawning.
    /// </summary>
    [DataField]
    public EntityWhitelist? BlockedByBlacklist;

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
