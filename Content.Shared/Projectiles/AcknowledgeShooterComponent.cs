using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Projectiles;

/// <summary>
/// Makes so a projectile doesn't ignore the shooter after a little delay after being shot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AcknowledgeShooterComponent : Component
{
    /// <summary>
    /// how much time to wait after being shot to set <see cref="ProjectileComponent.IgnoreShooter"/> to false
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// when to set <see cref="ProjectileComponent.IgnoreShooter"/> to false
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? WhenToStopIgnoringShooter;
}
