using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Item;
using Content.Shared.Interaction.Components;
using Content.Shared.Glue;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Interaction;
using Content.Server.Nutrition.Components;

namespace Content.Server.Glue
{
    public sealed class GlueSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly FoodSystem _food = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GlueComponent, AfterInteractEvent>(OnInteract);
        }

        // When glue bottle is used on item it will apply the glued and unremoveable components.
        private void OnInteract(EntityUid uid, GlueComponent component, AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            if (!args.CanReach || args.Target is not { Valid: true } target)
                return;

            if (HasComp<GluedComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("glue-failure", ("target", Identity.Entity(target, EntityManager))), args.User,
                args.User, PopupType.Medium);
                return;
            }


            if (HasComp<ItemComponent>(target))
            {
                _audio.PlayPvs(component.Squeeze, uid);
                EnsureComp<UnremoveableComponent>(target);
                _popup.PopupEntity(Loc.GetString("glue-success", ("target", Identity.Entity(target, EntityManager))), args.User,
                args.User, PopupType.Medium);
                EnsureComp<GluedComponent>(target);
            }

            if (TryComp<FoodComponent>(uid, out var food))
            {
                _food.DeleteAndSpawnTrash(food, uid, args.User);
            }

            args.Handled = true;
        }

    }
}
