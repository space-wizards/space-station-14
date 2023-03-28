using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Surgery.Components;

/// <summary>
/// Active surgical operation in progress on a mob.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(OperationSystem))]
public sealed class OperationComponent : Component
{
    /// <summary>
    /// The body part being operated on, will have <see cref="BodyPartComponent"/>.
    /// This restricts patients to one operation at a time, the surgeons have no restrictions.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Part;

    /// <summary>
    /// Prototype of the operation being done.
    /// </summary>
    [ViewVariables]
    public SurgeryOperationPrototype? Prototype;

    /// <summary>
    /// Tags for marking progress.
    /// </summary>
    [ViewVariables]
    public List<SurgeryTag> Tags = new();

    /// <summary>
    /// The organ selected for extraction
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? SelectedOrgan;

    /// <summary>
    /// Used to prevent doafter spamming.
    /// Restricts patients to one step being done at a time.
    /// </summary>
    [ViewVariables]
    public bool Busy;
}

[Serializable, NetSerializable]
public sealed class OperationComponentState : ComponentState
{
    public EntityUid Part;
    public List<SurgeryTag> Tags;
    public EntityUid? SelectedOrgan;

    public OperationComponentState(EntityUid part, List<SurgeryTag> tags, EntityUid? selectedOrgan)
    {
        Part = part;
        Tags = tags;
        SelectedOrgan = selectedOrgan;
    }
}
