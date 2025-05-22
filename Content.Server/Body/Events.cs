namespace Content.Server.Body;
/// <summary>
/// Subscribe to this event and set the
/// BloodOverrideColor to override blood
/// reagent color
/// </summary>
[ByRefEvent]
public record struct BloodColorOverrideEvent()
{
    public Color? OverrideColor;
}

[ByRefEvent]
public record struct RefreshBloodEvent()
{
}
[ByRefEvent]
public record struct ColorGibsEvent()
{
    public HashSet<EntityUid>? Gibs;
}

[ByRefEvent]
public record struct ColorGibPartEvent()
{
    public Color GibColor;
}
