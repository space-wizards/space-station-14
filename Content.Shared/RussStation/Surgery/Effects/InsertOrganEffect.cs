using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.RussStation.Surgery.Effects;

/// <summary>
/// Surgery effect: inserts an organ held in the surgeon's off-hand into the patient.
/// </summary>
[DataDefinition]
public sealed partial class InsertOrganEffect : ISurgeryEffect
{
}
