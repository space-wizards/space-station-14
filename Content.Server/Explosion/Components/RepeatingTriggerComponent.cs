using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Constantly triggers after being added to an entity.
/// </summary>
[RegisterComponent, Access(typeof(TriggerSystem))]
[AutoGenerateComponentPause]
public sealed partial class RepeatingTriggerComponent : Component
{
    /// <summary>
    /// How long to wait between triggers.
    /// The first trigger starts this long after the component is added.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// When the next trigger will be.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTrigger;
}
