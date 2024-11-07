using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Medical.Surgery.Events;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
/// <summary>
///     Raised on the entity that is receiving surgery.
/// </summary>
[ByRefEvent]
public record struct SurgeryValidEvent(EntityUid Body, EntityUid Part, bool Cancelled = false, string Suffix = "");
