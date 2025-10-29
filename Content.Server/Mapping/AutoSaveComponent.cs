using Content.Shared.CCVar;

namespace Content.Server.Mapping;

/// <summary>
/// Automatically serializes a map or a grid to the disk periodically.
/// The period is specified in <see cref="CCVars.AutosaveInterval"/> CVar.
/// </summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class AutoSaveComponent : Component
{
    /// <summary>
    /// Real time when this map/grid will get saved.
    /// </summary>
    /// <remarks>
    /// Since it stores real time and not simulation time, it runs out of sync with simulation.
    /// </remarks>
    [DataField]
    public TimeSpan NextSaveTime;

    /// <summary>
    /// Folder path in which this map will be saved.
    /// </summary>
    [DataField]
    public string FileName = string.Empty;
}
