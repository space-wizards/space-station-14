using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Laws;

/// <summary>
///     This event gets called whenever an AIs laws are actually updated.
/// </summary>
[ByRefEvent]
public record struct AILawUpdatedEvent(EntityUid Target, ProtoId<SiliconLawsetPrototype> Lawset);
