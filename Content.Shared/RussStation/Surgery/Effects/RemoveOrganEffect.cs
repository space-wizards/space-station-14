using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.RussStation.Surgery.Effects;

/// <summary>
/// Surgery effect: removes a selected organ from the patient and drops it.
/// </summary>
[DataDefinition]
public sealed partial class RemoveOrganEffect : ISurgeryEffect
{
}
