using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.BloodCult;

/// <summary>
/// Constants for Blood Cult
/// </summary>
public static class BloodCultConstants
{
    public static readonly ProtoId<NpcFactionPrototype> BloodCultistFactionId = "BloodCultist";
    /// Faction to restore when deconverting a cultist who has no stored original faction (e.g. roundstart blood cultists).
    public static readonly ProtoId<NpcFactionPrototype> DefaultDeconversionFaction = "NanoTrasen";
}
