using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Flash.Components;

/// <summary>
/// Marks an entity with the <see cref="FlashComponent"/> as currently flashing.
/// Only used for an Update loop for resetting the visuals.
/// </summary>
/// <remarks>
/// TODO: Replace this with something like sprite flick once that exists to get rid of the update loop.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedFlashSystem))]
public sealed partial class ActiveFlashComponent : Component
{
    /// <summary>
    /// Time at which this flash will be considered no longer active.
    /// At this time this component will be removed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan ActiveUntil = TimeSpan.Zero;
}
