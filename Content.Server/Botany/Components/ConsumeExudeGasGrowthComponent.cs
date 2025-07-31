using Content.Shared.Atmos;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class ConsumeExudeGasGrowthComponent : PlantGrowthComponent
{
    [DataField] public Dictionary<Gas, float> ConsumeGasses = new();
    [DataField] public Dictionary<Gas, float> ExudeGasses = new();
}
