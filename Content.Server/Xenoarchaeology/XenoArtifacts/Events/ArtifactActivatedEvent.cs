using Robust.Shared.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Events;

/// <summary>
///     Invokes when artifact was successfully activated.
///     Used to start attached effects.
/// </summary>
public class ArtifactActivatedEvent : EntityEventArgs
{
    /// <summary>
    ///     Entity that activate this artifact.
    ///     Usually player, but can also be another object.
    /// </summary>
    public EntityUid? Activator;
}
