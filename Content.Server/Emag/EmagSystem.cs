using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;

namespace Content.Server.Emag
{
    public sealed class EmagSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmagComponent, AfterInteractEvent>(OnAfterInteract);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var emag in EntityManager.EntityQuery<EmagComponent>())
            {
                if (emag.Charges == emag.MaxCharges)
                {
                    emag.Accumulator = 0;
                    continue;
                }

                emag.Accumulator += frameTime;

                if (emag.Accumulator < emag.RechargeTime)
                {
                    continue;
                }

                emag.Accumulator -= emag.RechargeTime;
                emag.Charges++;
            }
        }

        private void OnAfterInteract(EntityUid uid, EmagComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (_tagSystem.HasTag(args.Target.Value, "EmagImmune"))
                return;

            if (component.Charges <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-no-charges"), args.User, args.User);
                return;
            }

            var handled = DoEmag(args.Target, args.User);
            if (handled)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-success", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.User,
                    args.User, PopupType.Medium);
                _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(args.User):player} emagged {ToPrettyString(args.Target.Value):target}");
                component.Charges--;
                return;
            }
        }

        public bool DoEmag(EntityUid? target, EntityUid? user)
        {
            if (target == null || user == null)
                return false;

            var emaggedEvent = new GotEmaggedEvent(user.Value);
            RaiseLocalEvent(target.Value, emaggedEvent, false);
            return emaggedEvent.Handled;
        }
    }
}
