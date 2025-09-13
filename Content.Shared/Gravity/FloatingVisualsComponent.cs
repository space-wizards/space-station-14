using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

/// <summary>
/// Gives an entity a floating animation when weightless.
/// Requires <see cref="GravityAffectedComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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

    /// <summary>
    /// Is this entity currently floating?
    /// </summary>
    [ViewVariables]
    public bool IsFloating;

    /// <summary>
    /// The key the animation is identified with.
    /// </summary>
    public const string AnimationKey = "gravity";
}
