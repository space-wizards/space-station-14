using JetBrains.Annotations;

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
    /// A string whitelist which is checked against an <see cref="ISpriteSmoothState.Mask"/>
    /// If the mask contains this key, then that mask can smooth with this entity.
    /// </summary>
    [DataField(required: true)]
    public string Key { get; private set; }

    /// <summary>
    /// Array of <see cref="ISpriteSmoothState"/> which each apply custom smoothing for individual sprite layers.
    /// </summary>
    [DataField]
    public ISpriteSmoothState[] States { get; private set; } = [];

    /// <summary>
    ///     Prepended to the RSI state.
    /// </summary>
    [Obsolete]
    [DataField("base")]
    public string StateBase { get; set; } = string.Empty;
}
