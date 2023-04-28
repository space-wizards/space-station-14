using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Curves;

[Prototype("utilityCurvePreset")]
public sealed class UtilityCurvePresetPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField("curve", required: true)] public IUtilityCurve Curve = default!;
}
