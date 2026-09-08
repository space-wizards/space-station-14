using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedFloatingVisualizerSystem))]
public sealed partial class FloatingVisualsComponent : Component
{
    /// <summary>
    /// How long it takes to go from the bottom of the animation to the top.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AnimationTime = 2f;

    /// <summary>
    /// How far it goes in any direction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Offset = new(0, 0.2f);

    [DataField, AutoNetworkedField]
    public bool CanFloat;

    public readonly string AnimationKey = "gravity";
}
