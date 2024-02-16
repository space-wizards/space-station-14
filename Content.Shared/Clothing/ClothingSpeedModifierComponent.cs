using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent, Access(typeof(ClothingSpeedModifierSystem))]
public sealed partial class ClothingSpeedModifierComponent : Component
{
    [DataField("walkModifier", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public float WalkModifier = 1.0f;

    [DataField("sprintModifier", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public float SprintModifier = 1.0f;

    /// <summary>
    ///     Is this clothing item currently 'actively' slowing you down?
    ///     e.g. magboots can be turned on and off.
    /// </summary>
    [DataField("enabled")] public bool Enabled = true;
}

[Serializable, NetSerializable]
public sealed class ClothingSpeedModifierComponentState : ComponentState
{
    public float WalkModifier;
    public float SprintModifier;

    public bool Enabled;

    public ClothingSpeedModifierComponentState(float walkModifier, float sprintModifier, bool enabled)
    {
        WalkModifier = walkModifier;
        SprintModifier = sprintModifier;
        Enabled = enabled;
    }
}
