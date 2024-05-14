using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing;

/// <summary>
/// Modifies speed when worn.
/// Supports <c>ItemToggleComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ClothingSpeedModifierSystem))]
public sealed partial class ClothingSpeedModifierComponent : Component
{
    [DataField("walkModifier", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public float WalkModifier = 1.0f;

    [DataField("sprintModifier", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public float SprintModifier = 1.0f;
}

[Serializable, NetSerializable]
public sealed class ClothingSpeedModifierComponentState : ComponentState
{
    public float WalkModifier;
    public float SprintModifier;

    public ClothingSpeedModifierComponentState(float walkModifier, float sprintModifier)
    {
        WalkModifier = walkModifier;
        SprintModifier = sprintModifier;
    }
}
