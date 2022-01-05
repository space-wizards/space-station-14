using Robust.Shared.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Events;

public class ArtifactActivatedEvent : EntityEventArgs
{
    public EntityUid? User;
}
