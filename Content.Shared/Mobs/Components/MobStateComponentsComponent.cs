using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components;

/// <summary>
///     Used to specify components that should be automatically added/removed on mob state transitions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobStateComponentsComponent : Component
{
    /// <summary>
    ///     Specifies a list of components that should be added if a mob is in a given state.
    /// </summary>
    /// <example>
    /// components:
    ///   Critical:
    ///   - SomeCriticalComponent
    ///   Alive:
    ///   - SomeAliveComponent
    /// </example>
    [DataField("components")]
    public Dictionary<MobState, List<Type>> Components = new();

    [DataField] public List<Type> GrantedComponents = new();
}
