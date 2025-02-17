using Content.Server.Backmen.GameTicking.Rules.Components;
using Content.Shared.Backmen.Blob.Components;

namespace Content.Server.Backmen.Blob.Rule;

public sealed class BlobChangeLevelEvent : EntityEventArgs
{
    public Entity<BlobCoreComponent> BlobCore;
    public EntityUid Station;
    public BlobStage Level;
}
