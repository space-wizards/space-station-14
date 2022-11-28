using Content.Shared.Salvage;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage.Expeditions;

[Prototype("salvageExpedition")]
public sealed class SalvageExpeditionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("config", required: true)] public ISalvageMission Expedition = default!;

    [DataField("environment", required: true)]
    public SalvageEnvironment Environment = SalvageEnvironment.Caves;

    [DataField("minDuration")]
    public TimeSpan MinDuration = TimeSpan.FromSeconds(9 * 60);

    [DataField("maxDuration")]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(12 * 60);

    /// <summary>
    /// Available factions for selection for this mission prototype.
    /// </summary>
    [DataField("factions")]
    public List<string> Factions = new();
}
