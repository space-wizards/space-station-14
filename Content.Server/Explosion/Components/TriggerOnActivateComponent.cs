using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Triggers on click.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnActivateComponent : Component { 

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float CooldownTime { get; set; } = 0.5f;
    
    public TimeSpan LastTimeActivated = TimeSpan.Zero;
}
