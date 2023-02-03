using Robust.Client.Animations;
using Robust.Shared.Audio;

namespace Content.Client.Disposal.Visualizers;

[RegisterComponent]
[Access(typeof(DisposalUnitVisualizerSystem))]
public sealed class DisposalUnitVisualizerComponent : Component
{
    public const string AnimationKey = "disposal_unit_animation";

    [DataField("state_anchored", required: true)]
    public string? StateAnchored;

    [DataField("state_unanchored", required: true)]
    public string? StateUnAnchored;

    [DataField("state_charging", required: true)]
    public string? StateCharging;

    [DataField("overlay_charging", required: true)]
    public string? OverlayCharging;

    [DataField("overlay_ready", required: true)]
    public string? OverlayReady;

    [DataField("overlay_full", required: true)]
    public string? OverlayFull;

    [DataField("overlay_engaged", required: true)]
    public string? OverlayEngaged;

    [DataField("state_flush", required: true)]
    public string? StateFlush;

    [DataField("flush_sound", required: true)]
    public SoundSpecifier FlushSound = default!;

    [DataField("flush_time", required: true)]
    public float FlushTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public Animation FlushAnimation = default!;
}
