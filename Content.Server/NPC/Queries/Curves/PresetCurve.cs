namespace Content.Server.NPC.Queries.Curves;

public sealed class PresetCurve : IUtilityCurve
{
    [DataField("preset", required: true)] public readonly string Preset = default!;
}
