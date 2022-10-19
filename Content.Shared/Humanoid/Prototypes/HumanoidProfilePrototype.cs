using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

[Prototype("humanoidProfile")]
public readonly record struct HumanoidProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("customBaseLayers")]
    public readonly Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new();

    [DataField("profile")]
    public HumanoidCharacterProfile Profile { get; } = HumanoidCharacterProfile.Default();
}
