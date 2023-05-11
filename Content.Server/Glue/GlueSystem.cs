using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Item;
using Content.Shared.Interaction.Components;
using Content.Shared.Glue;

using Content.Shared.Interaction;

namespace Content.Server.Glue
{
    public sealed class GlueSystem : SharedGlueSystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GlueComponent, AfterInteractEvent>(OnInteract);
        }

        private void OnInteract(EntityUid uid, GlueComponent component, AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            if (!args.CanReach || args.Target is not { Valid: true } target)
                return;

            if (!HasComp<ItemComponent>(target))
                return;

            if (!HasComp<GluedComponent>(target) || !HasComp<UnremoveableComponent>(target))
            {

                EnsureComp<UnremoveableComponent>(target);
                _popup.PopupEntity(Loc.GetString("glue-success", ("target", Identity.Entity(target, EntityManager))), args.User,
                args.User, PopupType.Medium);
                EnsureComp<GluedComponent>(target);
            }

            args.Handled = true;
        }
    }
}
