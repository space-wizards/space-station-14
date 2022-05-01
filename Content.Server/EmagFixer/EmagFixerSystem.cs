using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;

namespace Content.Server.Emag
{
    public sealed class EmagFixerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAdminLogSystem _adminLog = default!;

        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmagFixerComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, EmagFixerComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (_tagSystem.HasTag(args.Target.Value, "EmagImmune"))
                return;

            if (component.Charges <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-no-charges"), args.User, Filter.Entities(args.User));
                return;
            }

            var fixedEvent = new GotEmaggedEvent(args.User, shouldFix: true);
            RaiseLocalEvent(args.Target.Value, fixedEvent, false);
            if (fixedEvent.Handled)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-fix-success", ("target", args.Target)), args.User, Filter.Entities(args.User));
                _adminLog.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(args.User):player} fixed emag on {ToPrettyString(args.Target.Value):target}");
                component.Charges--;
                return;
            }
        }
    }
}
