namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedOpenableSystem : EntitySystem
{
}

/// <summary>
/// Raised after an Openable is opened.
/// </summary>
[ByRefEvent]
public record struct OpenableOpenedEvent;

/// <summary>
/// Raised after an Openable is closed.
/// </summary>
[ByRefEvent]
public record struct OpenableClosedEvent;
