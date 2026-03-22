using Content.Server.Administration.Logs;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
namespace Content.Server.Photography;
public sealed class PhotographySystem: EntitySystem {
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IAdminLogManager _log = default!;
    public override void Initialize() {
        base.Initialize();
        SubscribeLocalEvent<PictureTakerComponent, MeleeHitEvent>(OnCameraMeleeHit);
        SubscribeLocalEvent<PhotographyComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, PhotographyComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
        {
            // cant see details from far away!
            return;
        }
        args.PushMessage(component.Text);
        args.PushText(Loc.GetString("photography-description", ("entity", component.Name)));

    }

    private void OnCameraMeleeHit(Entity<PictureTakerComponent> ent, ref MeleeHitEvent args) {
        if (!args.IsHit)
        {
            return;
        }
        foreach (var entity in args.HitEntities)
        {
            var text = _examine.GetExamineText(entity, ent.Owner);
            var name = Name(entity);
            var spawned = Spawn("Photography");
            var metadata = MetaData(spawned);
            var comp = new PhotographyComponent(name, text);
            AddComp(spawned, comp);
            _hands.PickupOrDrop(args.User, spawned, dropNear: true);
            // we only do the first entity
            return;
        }
    }

}
