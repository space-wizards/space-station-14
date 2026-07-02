using Content.Shared.Damage.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.Events;

/// <summary>
/// Raised on an entity with <see cref="DamageExaminableComponent"/> when examined to get the damage values displayed in the examine window.
/// </summary>
[ByRefEvent]
public readonly record struct DamageExamineEvent(FormattedMessage Message, EntityUid User);
