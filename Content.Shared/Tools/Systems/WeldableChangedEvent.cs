using Content.Shared.Tools.Components;

namespace Content.Shared.Tools.Systems;

/// <summary>
///     Raised when <see cref="WeldableComponent"/> has changed.
/// </summary>
[ByRefEvent]
public readonly record struct WeldableChangedEvent(bool IsWelded)
{
    public readonly bool IsWelded = IsWelded;
}
