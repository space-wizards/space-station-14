using Content.Server.Temperature.Components;
using Content.Server.Medical.Disease.Systems;
using Content.Shared.Medical.Disease;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Disease.Cures;

[DataDefinition]
public sealed partial class CureTemperature : CureStep
{
    /// <summary>
    /// Maximum allowed body temperature (K).
    /// </summary>
    [DataField("max")]
    public float MaxTemperature { get; private set; } = 309.15f; // 36 °C.

    /// <summary>
    /// Consecutive ticks required in range.
    /// </summary>
    [DataField]
    public int RequiredTicks { get; private set; } = 30;
}

public sealed partial class CureTemperature
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly DiseaseCureSystem _cureSystem = default!;

    /// <summary>
    /// Cures the disease after spending consecutive time within a temperature range.
    /// </summary>
    public override bool OnCure(EntityUid uid, DiseasePrototype disease)
    {
        if (!_entityManager.TryGetComponent(uid, out TemperatureComponent? temperature))
            return false;

        var state = _cureSystem.GetState(uid, disease.ID, this);
        if (temperature.CurrentTemperature > MaxTemperature)
        {
            state.Ticker = 0;
            return false;
        }

        state.Ticker++;
        return state.Ticker >= RequiredTicks;
    }

    public override IEnumerable<string> BuildDiagnoserLines(IPrototypeManager prototypes)
    {
        var defaultTickSeconds = new DiseaseCarrierComponent().TickDelay.TotalSeconds;
        var seconds = RequiredTicks * defaultTickSeconds;
        var maxK = Math.Round(MaxTemperature);
        var maxC = Math.Round(MaxTemperature - 273.15f);

        yield return Loc.GetString("diagnoser-cure-temp", ("max", maxK), ("maxC", maxC), ("time", seconds));
    }
}
