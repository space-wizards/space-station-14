using Content.Shared.Medical.Disease;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomVomit : SymptomBehavior
{
    /// <summary>
    /// Amount of thirst to add.
    /// </summary>
    [DataField]
    public float ThirstAdded = -40f;

    /// <summary>
    /// Amount of hunger to add.
    /// </summary>
    [DataField]
    public float HungerAdded = -40f;
}

public sealed partial class SymptomVomit
{
    [Dependency] private readonly VomitSystem _vomitSystem = default!;

    /// <summary>
    /// Forces the carrier to vomit. Used by food poisoning and similar symptoms.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        _vomitSystem.Vomit(uid, ThirstAdded, HungerAdded, true);
    }
}
