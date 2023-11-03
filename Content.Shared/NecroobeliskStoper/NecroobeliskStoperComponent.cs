using Content.Shared.Necroobelisk;
using Robust.Shared.Audio;

namespace Content.Shared.NecroobeliskStoper;

/// <summary>
/// This is used for scanning anomalies and
/// displaying information about them in the ui
/// </summary>
[RegisterComponent, Access(typeof(SharedNecroobeliskSystem))]
public sealed partial class NecroobeliskStoperComponent : Component
{

    [ViewVariables]
    public EntityUid? NecroobeliskStoper;
    /// <summary>
    /// How long the scan takes
    /// </summary>
    [DataField("scanDoAfterDuration")]
    public float ScanDoAfterDuration = 5;

    /// <summary>
    /// The sound plays when the scan finished
    /// </summary>
    [DataField("completeSound")]
    public SoundSpecifier? CompleteSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");
}

