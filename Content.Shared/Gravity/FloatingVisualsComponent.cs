using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedFloatingVisualizerSystem))]
public sealed class FloatingVisualsComponent : Component
{
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("animationTime")]
    public float AnimationTime = 2f;

    /// <summary>
    /// How far it goes in any direction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")]
    public Vector2 Offset = new(0, 0.2f);

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanFloat = false;
    public readonly string AnimationKey = "gravity";
}


[Serializable, NetSerializable]
public sealed class SharedFloatingVisualsComponentState : ComponentState
{
    public float AnimationTime;
    public Vector2 Offset;
    public bool HasGravity;

    public SharedFloatingVisualsComponentState(float animationTime, Vector2 offset, bool hasGravity)
    {
        AnimationTime = animationTime;
        Offset = offset;
        HasGravity = hasGravity;
    }
}
