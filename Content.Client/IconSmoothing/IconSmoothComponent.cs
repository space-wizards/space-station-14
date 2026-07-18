using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.IconSmoothing;

/// <summary>
///     Makes sprites of other grid-aligned entities like us connect.
/// </summary>
/// <remarks>
///     The system is based on Baystation12's smoothwalling, and thus will work with those.
///     To use, set <c>base</c> equal to the prefix of the corner states in the sprite base RSI.
///     Any objects with the same <c>key</c> will connect.
/// </remarks>
[RegisterComponent]
public sealed partial class IconSmoothComponent : Component
{
    [DataField]
    public bool Enabled = true;

    public (EntityUid?, Vector2i)? LastPosition;

    /// <summary>
    ///     Objects with this key can smooth with us, and vice versa.
    ///     TODO: YAML HELL, Make smoothing one way.
    /// </summary>
    [DataField("key", required: true)]
    public string SmoothKey { get; private set; }

    /// <summary>
    ///     We will attempt to smooth with any objects that contain these keys.
    ///     Only works one way, objects with these keys will not smooth with us
    /// </summary>
    [DataField]
    public List<string> AdditionalKeys = new();

    /// <summary>
    /// Index override for our iconsmooth layers. If null, layers will appear on top.
    /// </summary>
    [DataField]
    public int? Index;

    /// <summary>
    ///     Prepended to the RSI state.
    /// </summary>
    [DataField("base")]
    public string StateBase { get; set; } = string.Empty;

    [DataField]
    public ProtoId<ShaderPrototype>? Shader;

    /// <summary>
    ///     Mode that controls how the icon should be selected.
    /// </summary>
    [DataField]
    public IconSmoothingMode Mode = IconSmoothingMode.Corners;

    /// <summary>
    ///     Used by <see cref="IconSmoothSystem"/> to reduce redundant updates.
    /// </summary>
    internal int UpdateGeneration { get; set; }
}

/// <summary>
///     Controls the mode with which icon smoothing is calculated.
/// </summary>
[PublicAPI]
public enum IconSmoothingMode : byte
{
    /// <summary>
    ///     Each icon is made up of 4 corners, each of which can get a different state depending on
    ///     adjacent entities clockwise, counter-clockwise and diagonal with the corner.
    /// </summary>
    Corners,

    /// <summary>
    ///     There are 16 icons, only one of which is used at once.
    ///     The icon selected is a bit field made up of the cardinal direction flags that have adjacent entities.
    /// </summary>
    CardinalFlags,

    /// <summary>
    ///     The icon represents a triangular sprite with only 2 states, representing South / East being occupied or not.
    /// </summary>
    Diagonal,

    /// <summary>
    ///     Where this component contributes to our neighbors being calculated but we do not update our own sprite.
    /// </summary>
    NoSprite,
}
