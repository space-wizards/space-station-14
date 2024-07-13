using Content.Client.Xenoarchaeology.Ui;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.Equipment;

/// <summary>
/// This handles...
/// </summary>
public sealed class ArtifactAnalyzerSystem : SharedArtifactAnalyzerSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnalysisConsoleComponent, AfterAutoHandleStateEvent>(OnAnalysisConsoleAfterAutoHandleState);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, AfterAutoHandleStateEvent>(OnAnalyzerAfterAutoHandleState);
    }

    private void OnAnalysisConsoleAfterAutoHandleState(Entity<AnalysisConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<AnalysisConsoleBoundUserInterface>(ent.Owner, ArtifactAnalyzerUiKey.Key, out var bui))
            bui.Update(ent);
    }

    private void OnAnalyzerAfterAutoHandleState(Entity<ArtifactAnalyzerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryGetAnalysisConsole(ent, out var analysisConsole))
            return;

        if (_ui.TryGetOpenUi<AnalysisConsoleBoundUserInterface>(analysisConsole.Value.Owner, ArtifactAnalyzerUiKey.Key, out var bui))
            bui.Update(analysisConsole.Value);
    }
}
