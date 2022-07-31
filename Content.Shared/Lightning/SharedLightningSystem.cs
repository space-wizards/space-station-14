namespace Content.Shared.Lightning;

public class SharedLightningSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    //TODO: Add way to form the lightning (sprites/spawning)

    //TODO: Make fixture with shape (edge/AABB/poly) so the body and impact can shock

    //TODO: Scale the body of the sprite and the fixture.

    //TODO: Add Electrocution Component

    //TODO: Use TimedDespawn to handle the deletion

    //TODO: Add way to arc/chain lightning
}
