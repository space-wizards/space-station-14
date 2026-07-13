using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Drunk;

/// <summary>
/// This is used by a status effect entity to apply stumbling, causing the player to gradually move in the wrong direction.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WobblyWalkStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan TimeUntilMax = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The max angle in radians that the walk can be changed.
    /// </summary>
    [DataField]
    public Angle MaxAngle = MathF.PI / 2f;

    [DataField]
    public Angle CurrentAngle;

    /// <summary>
    /// The next time that <see cref="CurrentAngle"/> updates.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval at which this component updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);
}
