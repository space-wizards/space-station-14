namespace Content.Client.Paper;

[RegisterComponent]
public sealed class PaperVisualsComponent : Component
{
    // Background image
    //   Patch margins
    //   Modulate
    //   Tiling mode
    [DataField("backgroundImagePath")]
    public string? BackgroundImagePath;
    [DataField("backgroundPatchMargin")]
    public Box2? BackgroundPatchMargin;
    [DataField("backgroundModulate")]
    public Color? BackgroundModulate;
    [DataField("backgroundImageTile")]
    public bool BackgroundImageTile = false;

    // Header image
    //     Modulate
    [DataField("headerImagePath")]
    public string? HeaderImagePath;
    [DataField("headerImageModulate")]
    public Color? HeaderImageModulate;
    [DataField("headerMargin")]
    public Box2? HeaderMargin = null;

    //<todo.eoin Ensure all properties are used!

    // Footer image
    //     Modulate?
    //<todo.eoin

    // Content image - the actual part which is written on
    //   Modulate
    //   Stretch mode? (Might need X stretch controls separately?)
    [DataField("contentImagePath")]
    public string? ContentImagePath;

    [DataField("contentImageModulate")]
    public Color? ContentImageModulate;

    /// An additional margin around the content (including header)
    [DataField("contentMargin")]
    public Box2? ContentMargin;

    // Font color
    //<todo.eoin Feels like we want full control here, not an accent on the style?
    [DataField("fontAccentColor")]
    public string? FontAccentColor = null;

    [DataField("maxWritableArea")]
    public Vector2? MaxWritableArea = null;
}
