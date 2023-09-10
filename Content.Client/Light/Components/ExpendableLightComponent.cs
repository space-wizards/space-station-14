using Content.Client.Light.EntitySystems;
using Content.Shared.Light.Components;
using Robust.Shared.Audio;

namespace Content.Client.Light.Components;

/// <summary>
/// Component that represents a handheld expendable light which can be activated and eventually dies over time.
/// </summary>
[RegisterComponent]
public sealed partial class ExpendableLightComponent : SharedExpendableLightComponent
{
    /// <summary>
    /// The icon state used by expendable lights when the they have been completely expended.
    /// </summary>
    [DataField("iconStateSpent")]
    public string? IconStateSpent;

    /// <summary>
    /// The icon state used by expendable lights while they are lit.
    /// </summary>
    [DataField("iconStateLit")]
    public string? IconStateLit;

    /// <summary>
    /// The sprite layer shader used while the expendable light is lit.
    /// </summary>
    [DataField("spriteShaderLit")]
    public string? SpriteShaderLit = null;

    /// <summary>
    /// The sprite layer shader used after the expendable light has burnt out.
    /// </summary>
    [DataField("spriteShaderSpent")]
    public string? SpriteShaderSpent = null;

    /// <summary>
    /// The sprite layer shader used after the expendable light has burnt out.
    /// </summary>
    [DataField("glowColorLit")]
    public Color? GlowColorLit = null;

    /// <summary>
    /// The sound that plays when the expendable light is lit.
    /// </summary>
    [Access(typeof(ExpendableLightSystem))]
    public IPlayingAudioStream? PlayingStream;
}

public enum ExpendableLightVisualLayers : byte
{
    Base = 0,
    Glow = 1,
    Overlay = 2,
}
