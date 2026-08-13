using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Effects.Components;

/// <summary>
/// Marks a particle emitter as active and stores its client-side emission runtime state.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveParticleEmitterComponent : Component
{
    [ViewVariables]
    public EntityCoordinates LastPosition;

    [ViewVariables]
    public EntityCoordinates LastEmissionPosition;

    [ViewVariables]
    public TimeSpan NextEmissionTime;
}
