namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class AnalysisConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AnalyzerEntity;

    //TODO: figure this out later
    public readonly string ConsolePort = "ArtifactAnalyzerSender";
}
