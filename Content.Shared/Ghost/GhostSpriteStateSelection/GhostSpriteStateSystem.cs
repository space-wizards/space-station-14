namespace Content.Shared.Ghost.GhostSpriteStateSelection;

public sealed class GhostSpriteStateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostSpriteStateComponent, GhostSpriteEvent>(SetGhostSprite);
    }

    private void SetGhostSprite(Entity<GhostSpriteStateComponent> ent, ref GhostSpriteEvent args)
    {
        Log.Debug("do the thingies here yeah");

    }
}
