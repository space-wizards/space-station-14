using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Describes information about a specific disease.
/// </summary>
[Prototype("disease")]
public sealed partial class DiseasePrototype : IPrototype
{
    /// <summary>
    /// ID of the disease.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Displayed name of the disease.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Displayed description of the disease.
    /// </summary>
    [DataField("desc", required: true)]
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Spread vectors for this disease.
    /// </summary>
    [DataField(required: true)]
    public List<DiseaseSpreadFlags> SpreadFlags { get; private set; } = [];

    /// <summary>
    /// If true, this disease is considered beneficial for HUD purposes.
    /// Beneficial diseases show a buff icon on med HUD instead of an illness icon.
    /// </summary>
    [DataField]
    public bool IsBeneficial { get; private set; } = false;

    /// <summary>
    /// Probability of progression through disease stages per tick.
    /// </summary>
    [DataField]
    public float StageProb { get; private set; } = 0.02f;

    /// <summary>
    /// Stage configurations in ascending order (1-indexed semantics).
    /// Each stage can define stealth/resistance and symptom activations.
    /// </summary>
    [DataField(required: true)]
    public List<DiseaseStage> Stages { get; private set; } = [];

    /// <summary>
    /// Optional list of cure steps for the disease. Each entry is a specific cure action.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<CureStep> CureSteps { get; private set; } = [];

    /// <summary>
    /// Default immunity strength granted after curing this disease (0-1).
    /// </summary>
    [DataField]
    public float PostCureImmunity { get; private set; } = 0.7f;

    /// <summary>
    /// Base per-contact infection probability for this disease (0-1). Used when two entities make contact.
    /// </summary>
    [DataField]
    public float ContactInfect { get; private set; } = 0.1f;

    /// <summary>
    /// Amount of residue intensity deposited when a carrier with this disease contacts a surface.
    /// Expressed as (0-1) fraction added to per-disease residue intensity.
    /// </summary>
    [DataField]
    public float ContactDeposit { get; private set; } = 0.1f;

    /// <summary>
    /// Base per-target airborne infection probability (0-1) before PPE adjustments.
    /// </summary>
    [DataField]
    public float AirborneInfect { get; private set; } = 0.2f;

    /// <summary>
    /// Airborne infection radius in world units, used when <see cref="SpreadFlags"/> contains Airborne.
    /// </summary>
    [DataField]
    public float AirborneRange { get; private set; } = 2f;

    /// <summary>
    /// Per-tick chance (0-1) to attempt airborne spread from each carrier of this disease.
    /// </summary>
    [DataField]
    public float AirborneTickChance { get; private set; } = 0.3f;

    /// <summary>
    /// Optional incubation time in seconds before symptoms/spread begin after infection.
    /// </summary>
    [DataField]
    public float IncubationSeconds { get; private set; } = 0f;

    /// <summary>
    /// Per-disease permeability multiplier (0-1) applied to PPE/internals effectiveness.
    /// Values > 1 reduce protection; values < 1 increase protection.
    /// </summary>
    [DataField]
    public float PermeabilityMod { get; private set; } = 1.0f;
}

/// <summary>
/// Per-stage configuration for a disease.
/// </summary>
[DataDefinition]
public sealed partial class DiseaseStage
{
    /// <summary>
    /// Stage number (1-indexed).
    /// </summary>
    [DataField(required: true)]
    public int Stage { get; private set; } = 1;

    /// <summary>
    /// Optional stealth flags for this stage. Controls visibility in HUD/diagnoser/analyzers.
    /// TODO: does not work <see cref="DiseaseStealthFlags"/>
    /// </summary>
    [DataField]
    public DiseaseStealthFlags Stealth { get; private set; } = DiseaseStealthFlags.None;

    /// <summary>
    /// Symptoms that can trigger during this stage. Order matters for deterministic iteration.
    /// Each entry is a mapping with `symptom` and optional `probability` to override the symptom prototype's `probability`.
    /// </summary>
    [DataField]
    public List<SymptomEntry> Symptoms { get; private set; } = [];

    /// <summary>
    /// Optional list of localized message keys to show as "sensations" to the carrier while at this stage.
    /// A single entry is randomly picked on each eligible tick, controlled by <see cref="SensationProb"/>.
    /// </summary>
    [DataField]
    public List<SensationEntry> Sensations { get; private set; } = [];

    /// <summary>
    /// Optional list of cure steps specific to this stage. Overrides disease-level <see cref="CureSteps"/> for this stage.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<CureStep> CureSteps { get; private set; } = [];
}

[DataDefinition]
public sealed partial class SymptomEntry
{
    /// <summary>
    /// Symptom prototype ID to trigger.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DiseaseSymptomPrototype> Symptom { get; private set; } = default!;

    /// <summary>
    /// Per-tick probability (0-1) to trigger this symptom while in the stage. If negative, the probability of symptom is used.
    /// </summary>
    [DataField]
    public float Probability { get; private set; } = -1f;
}

[DataDefinition]
public sealed partial class SensationEntry
{
    /// <summary>
    /// Localization key for the popup text.
    /// </summary>
    [DataField(required: true)]
    public string Sensation { get; private set; } = default!;

    /// <summary>
    /// Popup visual style <see cref="PopupType"/>.
    /// </summary>
    [DataField]
    public PopupType PopupType { get; private set; } = PopupType.Small;

    /// <summary>
    /// Per-tick probability (0-1) to show this sensation popup.
    /// </summary>
    [DataField]
    public float Probability { get; private set; } = 0.05f;
}
