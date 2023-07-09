namespace Content.Server.Pinpointer;

[RegisterComponent]
public sealed class StationMapComponent : Component
{

}

/// <summary>
/// Added to an entity using station map so when its parent changes we reset it.
/// </summary>
[RegisterComponent]
public sealed class StationMapUserComponent : Component
{
    [DataField("mapUid")]
    public EntityUid Map;
}
