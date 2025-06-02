namespace Content.Shared.Ghost.GhostSpriteStateSelection;

public sealed class GhostSpriteEvent : EntityEventArgs
{
    public GhostSpriteEvent(EntityUid uid)
    {
        Uid = uid;
    }

    public EntityUid Uid { get; set; }
}
