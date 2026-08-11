using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chasm.Components;

/// <summary>
/// Added to entities which have started falling into an entity with <see cref="ChasmComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ChasmFallingComponent : Component
{
    /// <summary>
    /// The entity with <see cref="ChasmComponent"/> this entity is falling into.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid FallingInto;

    /// <summary>
    /// Time it should take for the falling animation (scaling down) to complete.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// Time it should take in seconds for the entity to actually delete
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EffectsTime = TimeSpan.FromSeconds(1.8f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextEffectsTime = TimeSpan.Zero;
}
