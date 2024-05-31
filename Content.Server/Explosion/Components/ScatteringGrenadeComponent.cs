using Content.Server.Explosion.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components;

[RegisterComponent, Access(typeof(ScatteringGrenadeSystem))]

/// <summary>
/// Use this component if the grenade splits into entities that make use of Timers
/// or if you just want it to throw entities out in the world
/// </summary>
public sealed partial class ScatteringGrenadeComponent : Component
{
    public Container Container = default!;

    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     What we fill our prototype with if we want to pre-spawn with entities.
    /// </summary>
    [DataField]
    public EntProtoId? FillPrototype;

    /// <summary>
    ///     If we have a pre-fill how many more can we spawn.
    /// </summary>
    public int UnspawnedCount;

    /// <summary>
    ///     Max amount of entities inside the container
    /// </summary>
    [DataField]
    public int Capacity = 3;

    /// <summary>
    ///     Decides if contained entities trigger after getting launched
    /// </summary>
    [DataField]
    public bool TriggerContents = true;

    /// <summary>
    ///     Minimum delay in seconds before any entities start to be triggered.
    /// </summary>
    [DataField]
    public float DelayBeforeTriggerContents = 1.0f;

    /// <summary>
    ///     Maximum delay in seconds to add between individual entity triggers
    /// </summary>
    [DataField]
    public float IntervalBetweenTriggersMax = 0f;

    /// <summary>
    ///     Minimum delay in seconds to add between individual entity triggers
    /// </summary>
    [DataField]
    public float IntervalBetweenTriggersMin = 0f;

    /// <summary>
    ///     Should the angle the entities get thrown at be random
    ///     instead of uniformly distributed
    /// </summary>
    [DataField]
    public bool RandomAngle = false;

    /// <summary>
    ///     The speed at which the entities get thrown
    /// </summary>
    [DataField]
    public float Velocity = 5;


    /// <summary>
    ///     Static distance grenades will be thrown to if RandomDistance is false.
    /// </summary>
    [DataField]
    public float Distance = 1f;

    /// <summary>
    ///     Should the distance the entities get thrown be random
    /// </summary>
    [DataField]
    public bool RandomDistance = false;

    /// <summary>
    ///     Max distance grenades should randomly be thrown to.
    /// </summary>
    [DataField]
    public float RandomThrowDistanceMax = 2.5f;

    /// <summary>
    ///     Minimal distance grenades should randomly be thrown to.
    /// </summary>
    [DataField]
    public float RandomThrowDistanceMin = 0f;

    /// <summary>
    ///     We store whether it's been triggered so that it can
    ///     be checked on the next frame update rather than
    ///     triggering on the TimerEvent itself (prevents crashing)
    /// </summary>
    public bool IsTriggered = false;
}
