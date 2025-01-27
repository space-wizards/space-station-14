using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Explosion.Components;

/// <summary>
/// Use this component if the grenade splits into entities that make use of Timers
/// or if you just want it to throw entities out in the world
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedScatteringGrenadeSystem))]
public sealed partial class ScatteringGrenadeComponent : Component
{
    public Container Container = default!;

    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// What we fill our prototype with if we want to pre-spawn with entities.
    /// </summary>
    [DataField]
    public EntProtoId? FillPrototype;

    /// <summary>
    /// If we have a pre-fill how many more can we spawn.
    /// </summary>
    [AutoNetworkedField]
    public int UnspawnedCount;

    /// <summary>
    /// Max amount of entities inside the container
    /// </summary>
    [DataField]
    public int Capacity = 3;

    /// <summary>
    /// Decides if contained entities trigger after getting launched
    /// </summary>
    [DataField]
    public bool TriggerContents = true;

    #region Trigger time parameters for scattered entities
    /// <summary>
    ///  Minimum delay in seconds before any entities start to be triggered.
    /// </summary>
    [DataField]
    public float DelayBeforeTriggerContents = 1.0f;

    /// <summary>
    /// Maximum delay in seconds to add between individual entity triggers
    /// </summary>
    [DataField]
    public float IntervalBetweenTriggersMax;

    /// <summary>
    /// Minimum delay in seconds to add between individual entity triggers
    /// </summary>
    [DataField]
    public float IntervalBetweenTriggersMin;
    #endregion

    #region Throwing parameters for the scattered entities
    /// <summary>
    /// Should the angle the entities get thrown at be random
    /// instead of uniformly distributed
    /// </summary>
    [DataField]
    public bool RandomAngle;

    /// <summary>
    /// The speed at which the entities get thrown
    /// </summary>
    [DataField]
    public float Velocity = 5;

    /// <summary>
    /// Static distance grenades will be thrown to if RandomDistance is false.
    /// </summary>
    [DataField]
    public float Distance = 1f;

    /// <summary>
    /// Should the distance the entities get thrown be random
    /// </summary>
    [DataField]
    public bool RandomDistance;

    /// <summary>
    /// Max distance grenades can randomly be thrown to.
    /// </summary>
    [DataField]
    public float RandomThrowDistanceMax = 2.5f;

    /// <summary>
    /// Minimal distance grenades can randomly be thrown to.
    /// </summary>
    [DataField]
    public float RandomThrowDistanceMin;
    #endregion

    /// <summary>
    /// Whether the main grenade has been triggered or not
    /// We need to store this because we are only allowed to spawn and trigger timed entities on the next available frame update
    /// </summary>
    public bool IsTriggered = false;
}
