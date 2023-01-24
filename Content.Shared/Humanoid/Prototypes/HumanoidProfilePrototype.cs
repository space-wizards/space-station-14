using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using static Content.Shared.Humanoid.HumanoidAppearanceState;

namespace Content.Shared.Humanoid.Prototypes;

[Prototype("humanoidProfile")]
public sealed class HumanoidProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("customBaseLayers")]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new();

    [DataField("profile")]
    public HumanoidCharacterProfile Profile { get; } = HumanoidCharacterProfile.Default();
}
