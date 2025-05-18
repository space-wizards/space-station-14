using Content.Server.Research.Systems;
using Content.Server.Xenoarchaeology.Artifact;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Xenoarchaeology.Equipment;

/// <inheritdoc />
public sealed class ArtifactAnalyzerSystem : SharedArtifactAnalyzerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly XenoArtifactSystem _xenoArtifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnalysisConsoleComponent, AnalysisConsoleExtractButtonPressedMessage>(OnExtractButtonPressed);
    }

    private void OnExtractButtonPressed(Entity<AnalysisConsoleComponent> ent, ref AnalysisConsoleExtractButtonPressedMessage args)
    {
        if (!TryGetArtifactFromConsole(ent, out var artifact))
            return;

        if (!_research.TryGetClientServer(ent, out var server, out var serverComponent))
            return;

        var sumResearch = 0;
        foreach (var node in _xenoArtifact.GetAllNodes(artifact.Value))
        {
            var research = _xenoArtifact.GetResearchValue(node);
            _xenoArtifact.SetConsumedResearchValue(node, node.Comp.ConsumedResearchValue + research);
            sumResearch += research;
        }

        // 4-16-25: It's a sad day when a scientist makes negative 5k research
        if (sumResearch <= 0)
            return;

        _research.ModifyServerPoints(server.Value, sumResearch, serverComponent);
        _audio.PlayPvs(ent.Comp.ExtractSound, artifact.Value);
        _popup.PopupEntity(Loc.GetString("analyzer-artifact-extract-popup"), artifact.Value, PopupType.Large);
    }
}

