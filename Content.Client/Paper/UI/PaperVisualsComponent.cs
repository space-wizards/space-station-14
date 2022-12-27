namespace Content.Client.Paper;

[RegisterComponent]
public sealed class PaperVisualsComponent : Component
{
    [DataField("borderTexturePath")]
    public string? BorderTexturePath = null;

    [DataField("borderCenterPatch")]
    public Box2? BorderCenterPatch = null;

    [DataField("contentPatch")]
    public Box2? ContentPatch = null;

    //<todo.eoin Probably want to use a Sprite here?
    [DataField("centerTexturePath")]
    public string? CenterTexturePath = null;


    // Background image
    //   Modulate
    //   Patch margins
    [DataField("backgroundImagePath")]
    public string? BackgroundImagePath;
    [DataField("backgroundPatchMargin")]
    public Box2? BackgroundPatchMargin;
    [DataField("backgroundModulate")]
    public Color? BackgroundModulate;

    [DataField("backgroundImageTile")]
    public bool BackgroundImageTile = false;

    // Header image(s?)
    //     Header alignment
    //     Modulate?
    [DataField("headerImagePath")]
    public string? HeaderImagePath;
    [DataField("headerImageAlignment")]
    public string? HeaderAlignment; // Seems I can't serialize a VAlignment?
    [DataField("headerImageModulate")]
    public Color? HeaderImageModulate;

    //<todo.eoin Ensure all properties are used!

    // Footer image
    //     Modulate?
    //<todo.eoin

    // Content image
    //   Modulate
    //   Stretch mode? (Might need X strech controls sperately?)
    [DataField("contentImagePath")]
    public string? ContentImagePath;

    [DataField("contentImageModulate")]
    public Color? ContentImageModulate;

    [DataField("contentMargin")]
    public Box2? ContentMargin;

    // Font color
    //<todo.eoin Feels like we want full control here, not an accent on the style?
    [DataField("fontAccentColor")]
    public string? FontAccentColor = null;
}
