using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Drunk;

/// <summary>
/// This is used by a status effect entity to apply a wobbly walk, causing the player to randomly move in the wrong direction.
/// <remarks>The effect scales linearly to its max strength, and then at the end of the status effect scales back down.</remarks>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(WobblyMovementSystem))]
public sealed partial class WobblyMovementStatusEffectComponent : Component
{
    /// <summary>
    /// How long it takes for the effect to scale up to its <see cref="MaxAngle"/> strength.
    /// Does not include <see cref="DelayBufferTime"/>.
    /// </summary>
    [DataField]
    public TimeSpan TimeUntilMax = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Buffer time before the effect starts kicking in, and making it end earlier; to prevent the effect to be felt immediately.
    /// </summary>
    [DataField]
    public TimeSpan DelayBufferTime = TimeSpan.FromSeconds(50);

    /// <summary>
    /// The max angle in radians that the walk can be changed.
    /// </summary>
    [DataField]
    public Angle MaxAngle = MathF.PI / 2f;

    /// <summary>
    /// The current angle we are using to rotate the player's movement.
    /// </summary>
    [AutoNetworkedField, DataField]
    public Angle CurrentAngle;

    /// <summary>
    /// The next time that <see cref="CurrentAngle"/> updates.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// A new <see cref="CurrentAngle"/> gets generated sometime between these values, in seconds.
    /// </summary>
    [DataField]
    public Vector2 UpdateIntervalIntervals = new(1f, 3f);
}
