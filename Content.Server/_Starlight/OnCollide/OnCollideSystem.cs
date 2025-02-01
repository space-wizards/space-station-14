using Robust.Shared.Physics.Events;
using Content.Shared._Starlight.OnCollide;

namespace Content.Server._Starlight.OnCollide;

public sealed partial class OnCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnOnCollideComponent, StartCollideEvent>(SpawnOnCollide);
        base.Initialize();
    }

    private void SpawnOnCollide(Entity<SpawnOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Collided) return;
        ent.Comp.Collided = true;
        SpawnAtPosition(ent.Comp.Prototype, Transform(args.OtherEntity).Coordinates);
    }
}
