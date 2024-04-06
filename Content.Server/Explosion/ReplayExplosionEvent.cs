using Content.Server.GameTicking.Replays;

namespace Content.Server.Explosion;

[Serializable, DataDefinition]
public sealed partial class ReplayExplosionEvent : ReplayEvent
{
    [DataField]
    public ReplayEventPlayer? Source;

    [DataField]
    public float Intensity;

    [DataField]
    public float Slope;

    [DataField]
    public float MaxTileIntensity;

    [DataField]
    public float TileBreakScale;

    [DataField]
    public int MaxTileBreak;

    [DataField]
    public bool CanCreateVacuum;

    [DataField]
    public string Type;
}
