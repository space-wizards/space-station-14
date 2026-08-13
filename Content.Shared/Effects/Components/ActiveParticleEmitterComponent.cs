using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Effects.Components;

/// <summary>
/// Marks a particle emitter as active and stores its client-side emission state.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveParticleEmitterComponent : Component
{
    [ViewVariables]
    public EntityCoordinates LastCoordinates;

    [ViewVariables]
    public TimeSpan NextEmissionTime;
}
