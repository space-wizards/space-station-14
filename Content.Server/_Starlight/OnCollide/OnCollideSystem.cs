using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.OnHit;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Atmos.Components;
using Robust.Shared.Physics.Events;
using Content.Shared._Starlight.OnCollide;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class OnCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnOnCollideComponent, StartCollideEvent>(SpawnOnCollide);
        base.Initialize();
    }

    private void SpawnOnCollide(Entity<SpawnOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if(ent.Comp.Collided) return;
        ent.Comp.Collided = true;
        SpawnAtPosition(ent.Comp.Prototype, Transform(args.OtherEntity).Coordinates);
    }
}
