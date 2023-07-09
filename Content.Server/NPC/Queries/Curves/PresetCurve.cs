using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NPC.Queries.Curves;

public sealed class PresetCurve : IUtilityCurve
{
    [DataField("preset", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<UtilityCurvePresetPrototype>))] public readonly string Preset = default!;
}
