namespace Content.Server.Body;

/// <summary>
/// Subscribe to this event and set the
/// BloodOverrideColor to override blood reagent color
/// </summary>
[ByRefEvent]
public record struct BloodColorOverrideEvent(Color? OverrideColor);

/// <summary>
/// Recalculates blood data like color and DNA when invoked.
/// </summary>
[ByRefEvent]
public record struct RefreshBloodEvent()
{
}

/// <summary>
/// Raised in response to BeingGibbedEvent to color gibs.
/// </summary>
[ByRefEvent]
public record struct ColorGibsEvent()
{
    public HashSet<EntityUid>? Gibs;
}

/// <summary>
/// When something is gibbed,
/// lots of things are expunged as a result, such as inventory items.
/// Not all of these things need to be tinted so this event
/// is used to selectively tint the individual gibs.
/// </summary>
/// <param name="GibColor"></param>
[ByRefEvent]
public record struct ColorGibPartEvent(Color GibColor);
