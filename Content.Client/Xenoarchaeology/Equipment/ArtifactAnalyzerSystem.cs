using Content.Client.Xenoarchaeology.Ui;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Xenoarchaeology.Equipment;

/// <inheritdoc />
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
        UpdateBuiIfCanGetAnalysisConsoleUi(ent);
    }

    private void OnAnalyzerAfterAutoHandleState(Entity<ArtifactAnalyzerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryGetAnalysisConsole(ent, out var analysisConsole))
            return;

        UpdateBuiIfCanGetAnalysisConsoleUi(analysisConsole.Value);
    }

    private void UpdateBuiIfCanGetAnalysisConsoleUi(Entity<AnalysisConsoleComponent> analysisConsole)
    {
        if (_ui.TryGetOpenUi<AnalysisConsoleBoundUserInterface>(analysisConsole.Owner, ArtifactAnalyzerUiKey.Key, out var bui))
            bui.Update(analysisConsole);
    }
}
