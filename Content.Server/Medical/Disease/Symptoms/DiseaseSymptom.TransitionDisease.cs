using Content.Server.Medical.Disease.Systems;
using Content.Shared.Medical.Disease;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomTransitionDisease : SymptomBehavior
{
    /// <summary>
    /// Target disease prototype ID to apply.
    /// </summary>
    [DataField(required: true)]
    public string Disease { get; private set; } = string.Empty;

    /// <summary>
    /// Starting stage for the new disease.
    /// </summary>
    [DataField]
    public int StartStage { get; private set; } = 1;
}

public sealed partial class SymptomTransitionDisease
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;

    /// <summary>
    /// Replaces the current disease with another disease prototype, starting at a given stage.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype current)
    {
        if (string.IsNullOrWhiteSpace(Disease) || Disease == current.ID)
            return;

        if (_entityManager.TryGetComponent(uid, out DiseaseCarrierComponent? carrier) &&
            carrier.ActiveDiseases.ContainsKey(current.ID))
        {
            carrier.ActiveDiseases.Remove(current.ID);
        }

        _diseaseSystem.Infect(uid, Disease, Math.Max(1, StartStage));
    }
}
