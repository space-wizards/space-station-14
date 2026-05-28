using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Effects.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParticleEmitterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// The effect that will be spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? EffectPrototype;

    [DataField, AutoNetworkedField]
    public float Cooldown = 0.3f;

    /// <summary>
    /// Distance the entity must travel before a new particle is spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxDistance = 0.7f;

    [ViewVariables, AutoNetworkedField]
    public EntityCoordinates LastCoordinates;

    [ViewVariables]
    public TimeSpan TargetTime = TimeSpan.Zero;
}
