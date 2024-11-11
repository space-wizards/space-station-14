using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.Starlight.OnHit;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class OnHitSystem : SharedOnHitSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<CuffsOnHitComponent, CuffsOnHitDoAfter>(OnCuffsOnHitDoAfter);
        base.Initialize();
    }
    private void OnCuffsOnHitDoAfter(Entity<CuffsOnHitComponent> ent, ref CuffsOnHitDoAfter args)
    {
        if (!args.Args.Target.HasValue || args.Handled || args.Cancelled) return;

        var user = args.Args.User;
        var target = args.Args.Target.Value;

        if (!TryComp<CuffableComponent>(target, out var cuffable) || cuffable.Container.Count != 0)
            return;

        args.Handled = true;

        var handcuffs = SpawnNextToOrDrop(ent.Comp.HandcuffProtorype, args.User);

        if (!_cuffs.TryAddNewCuffs(target, user, handcuffs, cuffable))
            QueueDel(handcuffs);
    }
}
