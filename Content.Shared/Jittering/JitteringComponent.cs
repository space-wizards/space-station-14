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
    /// How far the sprite will travel from the entity's actual position.
    /// </summary>
    /// <remarks> Not recommended to make this larger than 300. </remarks>
    [DataField, AutoNetworkedField]
    public float Amplitude { get; set; }

    /// <summary>
    /// How many jitters will be preformed per second.
    /// </summary>
    /// <remarks> Not recommended to make this larger than 10. </remarks>
    [DataField, AutoNetworkedField]
    public float Frequency { get; set; }

    /// <summary>
    /// The current position of the sprite.
    /// </summary>
    [ViewVariables]
    public Vector2 LastJitter { get; set; }

    // todo I don't like this :o(
    /// <summary>
    /// The offset that an entity had before jittering started,
    /// so that we can reset it properly.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 StartOffset = Vector2.Zero;
}
