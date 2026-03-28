using Content.Shared.Damage;
using Content.Shared.RussStation.Surgery.Effects;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.RussStation.Surgery;

/// <summary>
/// A single step in a surgical procedure.
/// </summary>
[DataDefinition]
public sealed partial class SurgeryStep
{
    /// <summary>
    /// Tag the held tool must have to perform this step.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    /// <summary>
    /// How long the DoAfter takes in seconds (before speed modifiers).
    /// </summary>
    [DataField]
    public float Duration = 2f;

    /// <summary>
    /// Locale string for the popup shown on step completion.
    /// </summary>
    [DataField]
    public string Popup = string.Empty;

    /// <summary>
    /// Damage dealt to the patient when this step completes.
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// Damage healed on the patient when this step completes.
    /// When <see cref="HealingTotal"/> is set, this defines which damage types are eligible
    /// and the total is distributed proportionally across actual damage.
    /// When HealingTotal is zero, each type heals independently by its listed amount.
    /// </summary>
    [DataField]
    public DamageSpecifier? Healing;

    /// <summary>
    /// If non-zero, caps the total healing across all types listed in <see cref="Healing"/>.
    /// The budget is distributed proportionally based on the patient's current damage.
    /// </summary>
    [DataField]
    public float HealingTotal;

    /// <summary>
    /// Modifier to the patient's bleed amount. Positive adds bleeding, negative reduces it.
    /// </summary>
    [DataField]
    public float BleedModifier;

    /// <summary>
    /// If true, this step can be repeated multiple times before advancing.
    /// </summary>
    [DataField]
    public bool Repeatable;

    /// <summary>
    /// The surgery effect to trigger when this step completes (e.g. organ removal).
    /// </summary>
    [DataField]
    public ISurgeryEffect? Effect;
}

/// <summary>
/// Prototype defining a surgical procedure as a sequence of tool steps.
/// </summary>
[Prototype]
public sealed partial class SurgeryProcedurePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField(required: true)]
    public List<SurgeryStep> Steps = new();
}
