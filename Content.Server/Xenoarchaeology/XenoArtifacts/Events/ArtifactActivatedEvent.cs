using Robust.Shared.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Events;

/// <summary>
///     Invokes when artifact was successfully activated.
///     Used to start attached effects.
/// </summary>
public class ArtifactActivatedEvent : EntityEventArgs
{
    public EntityUid? User;
}
