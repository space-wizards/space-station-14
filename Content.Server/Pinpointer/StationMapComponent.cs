namespace Content.Server.Pinpointer;

[RegisterComponent]
public sealed partial class StationMapComponent : Component
{

}

/// <summary>
/// Added to an entity using station map so when its parent changes we reset it.
/// </summary>
[RegisterComponent]
public sealed partial class StationMapUserComponent : Component
{
    [DataField("mapUid")]
    public EntityUid Map;
}
