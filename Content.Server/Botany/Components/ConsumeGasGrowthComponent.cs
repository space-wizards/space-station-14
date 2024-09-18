using Content.Shared.Atmos;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class ConsumeGasGrowthComponent : PlantGrowthComponent
{
    [DataField] public Dictionary<Gas, float> ConsumeGasses = new();
}
