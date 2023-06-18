// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.PipePainter;

// ReSharper disable RedundantLinebreak

[RegisterComponent]
public sealed class PipePainterComponent : Component
{

    /// <summary>
    /// Sound that plays when painting a pipe
    /// </summary>
    [DataField("spraySound")]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// How long it takes to paint a pipe
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sprayTime")]
    public float SprayTime = 1.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public string? PickedColor;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("colorPalette")]
    public Dictionary<string, Color> ColorPalette = new();
}
