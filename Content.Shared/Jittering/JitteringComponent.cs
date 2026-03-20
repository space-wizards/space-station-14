using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Jittering;

/// <summary>
///  Causes an entity's sprite to move erratically around their position.
///
///  Use only in conjunction with <see cref="StatusEffectComponent"/>, on the status effect entity.
/// </summary>
[Access(typeof(SharedJitteringSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JitteringComponent : Component
{
    /// <summary>
    /// How many jitters will be preformed per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Frequency = 3;

    /// <summary>
    /// Distance scalar.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Amplitude = 1;

    /// <summary>
    /// The maximum distance the sprite will travel from the entity's actual position.
    /// </summary>
    [DataField]
    public float MaxRadius = 0.25f;

    /// <summary>
    /// The minimum distance to travel from the origin.
    /// </summary>
    [DataField]
    public float MinRadius;

    /// <summary>
    /// A linear transformation to apply to X.
    /// </summary>
    [DataField]
    public Vector2 XSheer = Vector2.UnitX;

    /// <summary>
    /// A linear transformation to apply to Y.
    /// </summary>
    [DataField]
    public Vector2 YSheer = Vector2.UnitY;

    /// <summary>
    /// The current position of the sprite.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 LastJitter;

    // todo I don't like this :o(
    /// <summary>
    /// The offset that an entity had before jittering started,
    /// so that we can reset it properly.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 StartOffset = Vector2.Zero;
}
