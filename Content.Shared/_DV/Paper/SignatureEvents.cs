using Content.Shared.Paper;

namespace Content.Shared.DV.Paper;

/// <summary>
/// Raised on the pen when trying to sign a paper.
/// If it's cancelled the signature isn't made.
/// </summary>
[ByRefEvent]
public record struct SignAttemptEvent(Entity<PaperComponent> Paper, EntityUid User, EntityUid Pen, bool Cancelled = false);
