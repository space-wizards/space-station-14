using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunnedComponent : Component
{
    [AutoNetworkedField, DataField]
    public float Amplitude = 0.1f;

    [AutoNetworkedField, DataField]
    public float Frequency = 8f;

    [AutoNetworkedField, DataField]
    public Vector2 LastJitter { get; set; }

    /// <summary>
    ///     The offset that an entity had before jittering started,
    ///     so that we can reset it properly.
    /// </summary>
    [AutoNetworkedField, DataField]
    public Vector2 StartOffset = Vector2.Zero;
}
