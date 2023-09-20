using Robust.Shared.Audio;

namespace Content.Server.SprayPainter;

[RegisterComponent]
public sealed partial class SprayPainterComponent : Component
{
    [DataField("spraySound")]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    [DataField("airlockSprayTime")]
    public float AirlockSprayTime = 3.0f;

    [DataField("pipeSprayTime")]
    public float PipeSprayTime = 1.0f;

    [DataField("isSpraying")]
    public bool IsSpraying = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public string? PickedColor;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("colorPalette")]
    public Dictionary<string, Color> ColorPalette = new();

    public int Index = default!;
}
