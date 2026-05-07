using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chasm.Components;

/// <summary>
///     Added to entities which have started falling into a chasm.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas:true), AutoGenerateComponentPause]
public sealed partial class ChasmFallingComponent : Component
{
    /// <summary>
    ///     Time it should take for the falling animation (scaling down) to complete.
    /// </summary>
    [DataField]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     Time it should take in seconds for the entity to actually delete
    /// </summary>
    [DataField]
    public TimeSpan DeletionTime = TimeSpan.FromSeconds(1.8f);

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDeletionTime = TimeSpan.Zero;

    /// <summary>
    /// Chasm this entity is currently falling into.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? FallChasm;
}
