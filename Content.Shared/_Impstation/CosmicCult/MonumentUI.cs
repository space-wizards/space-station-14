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
    public int CurrentProgress;
    public int TargetProgress;
    public float ProgressOffset;
    public ProtoId<GlyphPrototype> SelectedGlyph;
    public HashSet<ProtoId<GlyphPrototype>> UnlockedGlyphs;

    public MonumentBuiState(int currentProgress, int targetProgress, float progressOffset, ProtoId<GlyphPrototype> selectedGlyph, HashSet<ProtoId<GlyphPrototype>> unlockedGlyphs)
    {
        CurrentProgress = currentProgress;
        TargetProgress = targetProgress;
        ProgressOffset = progressOffset;
        SelectedGlyph = selectedGlyph;
        UnlockedGlyphs = unlockedGlyphs;
    }

    public MonumentBuiState(MonumentComponent comp)
    {
        CurrentProgress = comp.CurrentProgress;
        TargetProgress = comp.TargetProgress;
        ProgressOffset = comp.ProgressOffset;
        SelectedGlyph = comp.SelectedGlyph;
        UnlockedGlyphs = comp.UnlockedGlyphs;
    }
}
