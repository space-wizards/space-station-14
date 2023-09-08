using Robust.Shared.Audio;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for tracking artifacts that are currently
/// being scanned by <see cref="ActiveArtifactAnalyzerComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class ActiveScannedArtifactComponent : Component
{
    /// <summary>
    /// The scanner that is scanning this artifact
    /// </summary>
    [ViewVariables]
    public EntityUid Scanner;

    /// <summary>
    /// The sound that plays when the scan fails
    /// </summary>
    public readonly SoundSpecifier ScanFailureSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}
