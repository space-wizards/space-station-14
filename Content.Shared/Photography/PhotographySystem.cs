using System.Linq;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Photography;
public sealed class PhotographySystem: Robust.Shared.GameObjects.EntitySystem {
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly EntityTableSystem _tables = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
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
        // this rng should be the same on the client and on the server
        var rng = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner));
        var tableResult = _tables.GetSpawns(ent.Comp.Photographs, rng);
        // so we dont have multiple enumeration
        var entProtoIds = tableResult.ToList();
        // no photographs...
        if (!entProtoIds.Any())
        {
            return;
        }
        {

            if (_charges.IsEmpty(ent.Owner))
            {

                // no charges, we can't print anymore
                return;

            }
        }
        foreach (var entity in args.HitEntities)
        {
            var text = _examine.GetExamineText(entity, ent.Owner);
            var name = Name(Identity.Entity(entity, _ent));

            foreach (var prototype in entProtoIds)
            {
                // we generate an individual photograph (there should be only one tough)
                var spawned = Spawn(prototype);
                var metadata = MetaData(spawned);
                var comp = new PhotographComponent
                {
                    Name = name,
                    Text = text,
                };
                AddComp(spawned, comp);
                _hands.PickupOrDrop(args.User, spawned, dropNear: true);
            }
            // we only do the first entity
            return;
        }
    }

}
