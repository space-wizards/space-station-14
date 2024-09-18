using Content.Shared.Atmos;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class ExudeGasGrowthComponent : PlantGrowthComponent
{
    [DataField] public Dictionary<Gas, float> ExudeGasses = new();
}
