using System.Linq;
using Content.Shared.Charges.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server.Photography;
public sealed class PhotographySystem: EntitySystem {
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<PictureTakerComponent, MeleeHitEvent>(OnCameraMeleeHit);
        SubscribeLocalEvent<PhotographComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, PhotographComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
        {
            // can't see details from far away!
            return;
        }
        // writes the description since we are close enough
        using (args.PushGroup("photographDescription"))
        {
            args.PushText(Loc.GetString("photograph-description", ("entity", component.Name)));
            args.PushMessage(component.Text);
        }

    }

    private void OnCameraMeleeHit(Entity<PictureTakerComponent> ent, ref MeleeHitEvent args) {
        // nothing was hit
        if (!args.IsHit)
        {
            return;
        }

        // no photographs...
        if (!ent.Comp.Photographs.Any())
        {
            return;
        }
        {
            if (TryComp(ent.Owner, out LimitedChargesComponent? comp))
            {
                if (comp.LastCharges == 0)
                {
                    // no charges, we can't print anymore
                    return;
                }
            }
        }
        foreach (var entity in args.HitEntities)
        {
            var text = _examine.GetExamineText(entity, ent.Owner);
            var name = Name(entity);


            var spawned = Spawn(ent.Comp.Photographs[_rng.Next(0, ent.Comp.Photographs.Count)]);
            var metadata = MetaData(spawned);
            var comp = new PhotographComponent(name, text);
            AddComp(spawned, comp);
            _hands.PickupOrDrop(args.User, spawned, dropNear: true);
            // we only do the first entity
            return;
        }
    }

}
