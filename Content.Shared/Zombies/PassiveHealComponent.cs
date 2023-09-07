using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Zombies;

/// <summary>
///   For providing a flat heal each second to a living mob. Currently only used by zombies.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PassiveHealSystem))]
public sealed partial class PassiveHealComponent : Component
{
    /// <summary>
    /// Specific healing points per second. Specify negative values to heal.
    /// </summary>
    [DataField("healPerSec")]
    public DamageSpecifier HealPerSec = default!;

    /// <summary>
    /// Next time we are going to apply a heal
    /// </summary>
    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// Healing stops permanently when given mobstate is entered
    /// </summary>
    [DataField("cancelState")]
    public MobState? CancelState = MobState.Critical;

}
