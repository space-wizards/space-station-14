using Content.Shared.Atmos;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class ConsumeExudeGasGrowthComponent : PlantGrowthComponent
{
    /// <summary>
    /// Dictionary of gases and their consumption rates per growth tick.
    /// </summary>
    [DataField] public Dictionary<Gas, float> ConsumeGasses = new();

    /// <summary>
    /// Dictionary of gases and their exude rates per growth tick.
    /// </summary>
    [DataField] public Dictionary<Gas, float> ExudeGasses = new();
}
