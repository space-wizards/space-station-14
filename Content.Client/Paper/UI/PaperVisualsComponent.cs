namespace Content.Client.Paper;

[RegisterComponent]
public sealed class PaperVisualsComponent : Component
{
    /// The path to the image which will be used as a background for the paper itself
    [DataField("backgroundImagePath")]
    public string? BackgroundImagePath;

    /// An optional patch to configure tiling stretching of the background. Used to set
    /// the PatchMargin in a `StyleBoxTexture`
    [DataField("backgroundPatchMargin")]
    public Box2 BackgroundPatchMargin = default;

    /// Modulate the background image by this color. Can be used to add colorful
    /// variants of images, without having to create new textures.
    [DataField("backgroundModulate")]
    public Color BackgroundModulate = Color.White;

    /// Should the background image tile, or be streched? See `StyleBoxTexture.StrechMode`
    [DataField("backgroundImageTile")]
    public bool BackgroundImageTile = false;

    /// An additional scale to apply to the background image
    [DataField("backgroundScale")]
    public Vector2 BackgroundScale = Vector2.One;

    /// A path to an image which will be used as a header on the paper
    [DataField("headerImagePath")]
    public string? HeaderImagePath;

    /// Modulate the header image by this color
    [DataField("headerImageModulate")]
    public Color HeaderImageModulate = Color.White;

    /// Any additional margin to add around the header
    [DataField("headerMargin")]
    public Box2 HeaderMargin = default;

    /// Path to an image to use as the background to the "content" of the paper
    /// The header and actual written text will use this as a background. The
    /// image will be tiled vertically with the property that the bottom of the
    /// written text will line up with the bottom of this image.
    [DataField("contentImagePath")]
    public string? ContentImagePath;

    /// Modulate the content image by this color
    [DataField("contentImageModulate")]
    public Color ContentImageModulate = Color.White;

    /// An additional margin around the content (including header)
    [DataField("contentMargin")]
    public Box2 ContentMargin = default;

    /// The number of lines that the content image represents. The
    /// content image will be vertically tiled after this many lines
    /// of text.
    [DataField("contentImageNumLines")]
    public int ContentImageNumLines = 1;

    /// Modulate the style's font by this color
    [DataField("fontAccentColor")]
    public Color FontAccentColor = Color.White;

    /// This can enforce that your paper has a limited area to write in.
    /// This will be scaled according to UI scale
    [DataField("maxWritableArea")]
    public Vector2? MaxWritableArea = null;
}
