namespace Content.Server.CrewManifest;

[RegisterComponent]
public class ActiveCrewManifestViewerComponent : Component
{
    /// <summary>
    ///     Station this entity is currently viewing the crew manifest from.
    /// </summary>
    public EntityUid? Station { get; set; }

    /// <summary>
    ///     The amount of viewers this entity has viewing the crew manifest
    ///     from itself. If this is zero, this component should be
    ///     removed.
    /// </summary>
    public uint Viewers { get; set; }
}
