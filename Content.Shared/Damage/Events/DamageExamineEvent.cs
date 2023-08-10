using Robust.Shared.Utility;

namespace Content.Shared.Damage.Events;

[ByRefEvent]
public readonly record struct DamageExamineEvent(FormattedMessage Message, EntityUid User);
