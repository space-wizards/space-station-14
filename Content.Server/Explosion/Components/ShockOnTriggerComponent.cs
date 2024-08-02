using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Explosion.Components;

/// <summary>
/// A component that electrocutes an entity having this component when a trigger is triggered.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ShockOnTriggerComponent : Component
{
    /// <summary>
    /// The force of an electric shock when the trigger is triggered.
    /// </summary>
    [ViewVariables]
    [DataField]
    public int Damage = 5;

    /// <summary>
    /// Duration of electric shock when the trigger is triggered.
    /// </summary>
    [ViewVariables]
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The minimum delay between repeating triggers.
    /// </summary>
    [ViewVariables]
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(4);

    /// <summary>
    /// When can the trigger run again?
    /// </summary>
    [ViewVariables]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextTrigger = TimeSpan.Zero;
}
