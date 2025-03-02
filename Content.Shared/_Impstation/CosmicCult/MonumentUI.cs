using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Cosmiccult;

[Serializable, NetSerializable]
public enum MonumentKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MonumentBuiState : BoundUserInterfaceState
{
    public int AvailableEntropy;
    public int EntropyUntilNextStage;
    public int CrewToConvertUntilNextStage;
    public float PercentageComplete;
    public ProtoId<GlyphPrototype> SelectedGlyph;
    public HashSet<ProtoId<InfluencePrototype>> UnlockedInfluences;
    public HashSet<ProtoId<GlyphPrototype>> UnlockedGlyphs;
    public MonumentBuiState(int availableEntropy, int entropyUntilNextStage, int crewToConvertUntilNextStage, float percentageComplete, ProtoId<GlyphPrototype> selectedGlyph, HashSet<ProtoId<InfluencePrototype>> unlockedInfluences, HashSet<ProtoId<GlyphPrototype>> unlockedGlyphs)
    {
        AvailableEntropy = availableEntropy;
        EntropyUntilNextStage = entropyUntilNextStage;
        CrewToConvertUntilNextStage = crewToConvertUntilNextStage;
        PercentageComplete = percentageComplete;
        SelectedGlyph = selectedGlyph;
        UnlockedInfluences = unlockedInfluences;
        UnlockedGlyphs = unlockedGlyphs;
    }

    public MonumentBuiState(MonumentComponent comp)
    {
        AvailableEntropy = comp.AvailableEntropy;
        EntropyUntilNextStage = comp.EntropyUntilNextStage;
        CrewToConvertUntilNextStage = comp.CrewToConvertNextStage;
        PercentageComplete = comp.PercentageComplete;
        SelectedGlyph = comp.SelectedGlyph;
        UnlockedInfluences = comp.UnlockedInfluences;
        UnlockedGlyphs = comp.UnlockedGlyphs;
    }
}
