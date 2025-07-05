using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBedSystem))]
public sealed partial class StasisBedComponent : Component
{
    /// <summary>
    /// What the metabolic update rate will be multiplied by (higher = slower metabolism)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] // Writing it is not supported. ApplyMetabolicMultiplierEvent needs to be refactored first
    [DataField, AutoNetworkedField]
    public float Multiplier = 10f;
}
