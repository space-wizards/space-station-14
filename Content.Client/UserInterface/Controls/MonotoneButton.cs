using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class MonotoneButton : Button
{
    /// <summary>
    /// Specifies the color of the button's background element
    /// </summary>
    public Color BackgroundColor { set; get; } = new Color(0.2f, 0.2f, 0.2f);

    /// <summary>
    /// Describes the general shape of the button (i.e., open vs closed).
    /// </summary>
    public MonotoneButtonShape Shape
    {
        get { return _shape; }
        set { _shape = value; UpdateAppearance(); }
    }

    private MonotoneButtonShape _shape = MonotoneButtonShape.Closed;

    // Unfilled buttons
    // Since the texture isn't uniform, we can't subsample it to make buttons
    // of different shapes, we need to use a separate texture for each
    private string[] _buttons =
        ["/Textures/Interface/Nano/Monotone/monotone_button.svg.96dpi.png",
        "/Textures/Interface/Nano/Monotone/monotone_button_open_left.svg.96dpi.png",
        "/Textures/Interface/Nano/Monotone/monotone_button_open_right.svg.96dpi.png",
        "/Textures/Interface/Nano/Monotone/monotone_button_open_both.svg.96dpi.png"];

    // Filled buttons
    // Let's just treat these the same as the unfilled buttons to ensure consistency
    private string[] _buttonsFilled =
        ["/Textures/Interface/Nano/Monotone/monotone_button_filled.svg.96dpi.png",
        "/Textures/Interface/Nano/Monotone/monotone_button_open_left_filled.svg.96dpi.png",
        "/Textures/Interface/Nano/Monotone/monotone_button_open_right_filled.svg.96dpi.png",
        "/Textures/Interface/Nano/Monotone/monotone_button_open_both_filled.svg.96dpi.png"];

    private readonly IResourceCache _resourceCache;

    public MonotoneButton()
    {
        IoCManager.InjectDependencies(this);

        _resourceCache = IoCManager.Resolve<IResourceCache>();

        Initialize();
        UpdateAppearance();
    }

    private void Initialize()
    {
        // Apply button texture
        var buttonbase = new StyleBoxTexture();
        buttonbase.SetPatchMargin(StyleBox.Margin.All, 11);
        buttonbase.SetPadding(StyleBox.Margin.All, 1);
        buttonbase.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        buttonbase.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);
        buttonbase.Texture = _resourceCache.GetTexture(_buttons[(int)Shape]);

        // We don't want any generic button styles being applied
        this.StyleBoxOverride = buttonbase;
    }

    private void UpdateAppearance()
    {
        if (_resourceCache == null)
            return;

        // Recolor label
        if (Label != null)
            Label.ModulateSelfOverride = Pressed ? BackgroundColor : null;

        // Get button texture
        var buttonTexture = Pressed ? _buttonsFilled[(int)Shape] : _buttons[(int)Shape];

        // Apply button texture
        if (StyleBoxOverride is StyleBoxTexture { } styleBoxTexture)
            styleBoxTexture.Texture = _resourceCache.GetTexture(buttonTexture);

        // Appearance modulations
        Modulate = Disabled ? Color.Gray : Color.White;
    }

    protected override void DrawModeChanged()
    {
        UpdateAppearance();
    }
}

public enum MonotoneButtonShape : byte
{
    Closed = 0,
    OpenLeft = 1,
    OpenRight = 2,
    OpenBoth = 3
}
