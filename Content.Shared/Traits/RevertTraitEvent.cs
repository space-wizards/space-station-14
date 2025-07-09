namespace Content.Shared.Traits;

/// <summary>
/// Raised on an entity when their traits are reverted.
/// </summary>
[ByRefEvent]
public record struct RevertTraitEvent(List<Type> Components)
{
}
