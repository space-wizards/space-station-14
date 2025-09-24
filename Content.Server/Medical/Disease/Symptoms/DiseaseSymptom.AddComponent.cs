using Content.Shared.Medical.Disease;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomAddComponent : SymptomBehavior
{
    /// <summary>
    /// Component registration name to add to the carrier.
    /// </summary>
    [DataField(required: true)]
    public string Component { get; private set; } = string.Empty;
}

public sealed partial class SymptomAddComponent
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <summary>
    /// Adds a permanent component to the carrier.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        if (string.IsNullOrWhiteSpace(Component))
            return;

        if (!_entityManager.ComponentFactory.TryGetRegistration(Component, out var reg))
            return;

        if (_entityManager.HasComponent(uid, reg.Type))
            return;

        var comp = (Component)_entityManager.ComponentFactory.GetComponent(Component);
        _entityManager.AddComponent(uid, comp);

        if (!_entityManager.TryGetComponent(uid, out DiseaseCarrierComponent? carrier))
            return;

        if (!carrier.AddedComponents.TryGetValue(disease.ID, out var set))
        {
            set = new HashSet<string>();
            carrier.AddedComponents[disease.ID] = set;
        }
        set.Add(Component);
    }

    /// <summary>
    /// Removes the component after cure.
    /// </summary>
    public override void OnDiseaseCured(EntityUid uid, DiseasePrototype disease)
    {
        if (!_entityManager.TryGetComponent(uid, out DiseaseCarrierComponent? carrier))
            return;

        if (!carrier.AddedComponents.TryGetValue(disease.ID, out var comps))
            return;

        foreach (var regName in comps)
        {
            if (_entityManager.ComponentFactory.TryGetRegistration(regName, out var reg) && _entityManager.HasComponent(uid, reg.Type))
                _entityManager.RemoveComponent(uid, reg.Type);
        }

        comps.Clear();
        carrier.AddedComponents.Remove(disease.ID);
    }

    public override void OnSymptomCured(EntityUid uid, DiseasePrototype disease, string symptomId)
    {
        OnDiseaseCured(uid, disease);
    }
}
