using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Curves;

public sealed partial class PresetCurve : IUtilityCurve
{
    [DataField(required: true)]
    public ProtoId<UtilityCurvePresetPrototype> Preset = default!;
}
