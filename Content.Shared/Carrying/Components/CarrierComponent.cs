using Content.Shared.Carrying.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Carrying.Components;

/// <summary>
/// Indicates this entity can fireman carry another entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCarryingSystem))]
public sealed partial class CarrierComponent : Component
{
    [AutoNetworkedField, DataField]
    public EntityUid? Carrying;

    [DataField]
    public float WalkSpeedModifier = 0.75f;

    [DataField]
    public float SprintSpeedModifier = 0.6f;

    /// <summary>
    /// Y offset for the carried entity visual. Overridable per prototype for different species heights.
    /// </summary>
    [DataField]
    public float CarryOffset = 0.2f;
}
