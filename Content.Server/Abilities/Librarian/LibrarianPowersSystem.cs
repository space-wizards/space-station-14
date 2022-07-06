using Content.Server.Abilities.Mime;
using Content.Server.Popups;
using Content.Server.Spawners.Components;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Roles;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Abilities.Librarian
{
    public sealed class LibrarianPowersSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private EntityCoordinates? libraryCoordinates;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LibrarianPowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<LibrarianPowersComponent, ShushActionEvent>(OnShush);
        }

        private void OnComponentInit(EntityUid uid, LibrarianPowersComponent component, ComponentInit args)
        {
            //find location of library for power range
            foreach (var spawner in _entityManager.EntityQuery<SpawnPointComponent>(false))
            {
                if (spawner.Job?.ID.Equals("Librarian") == true && TryComp<TransformComponent>(spawner.Owner, out var spawnerXform))
                {
                    libraryCoordinates = spawnerXform.Coordinates;
                    break;
                }
            }
        }

        //TODO: force them to whisper instead of muting?
        private void OnShush(EntityUid uid, LibrarianPowersComponent component, ShushActionEvent args)
        {
            //Can be muted
            if (!_statusSystem.CanApplyEffect(args.Target, "Muted"))
                return;

            //Already muted
            if (HasComp<MutedComponent>(args.Target) || (TryComp<MimePowersComponent>(args.Target, out var mime) && !mime.VowBroken))
                return;

            //Within the librarian's domain
            if (component.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("librarian-shush-popup", ("librarian", args.Performer),
                    ("target", args.Target)), args.Performer, Filter.Pvs(args.Performer));
                _popupSystem.PopupEntity(Loc.GetString("librarian-been-shushed"), args.Target, Filter.Entities(args.Target));

                _entityManager.AddComponent<MutedComponent>(args.Target);
                _alertsSystem.ShowAlert(args.Target, AlertType.Muted, null, (component.ShushTime, component.ShushTime));
                component.ShushedEntity = args.Target;
                component.Accumulator = 0f;
                args.Handled = true;
            }
        }

        private void EnterLibraryDomain(LibrarianPowersComponent component)
        {
            component.Enabled = true;
            _alertsSystem.ShowAlert(component.Owner, AlertType.LibraryDomain);
            _actionsSystem.AddAction(component.Owner, component.ShushAction, component.Owner);
            _popupSystem.PopupEntity(Loc.GetString("librarian-gain-powers"), component.Owner, Filter.Entities(component.Owner));
        }

        private void LeaveLibraryDomain(LibrarianPowersComponent component)
        {
            component.Enabled = false;
            _alertsSystem.ClearAlert(component.Owner, AlertType.LibraryDomain);
            _actionsSystem.RemoveAction(component.Owner, component.ShushAction);
            _popupSystem.PopupEntity(Loc.GetString("librarian-lose-powers"), component.Owner, Filter.Entities(component.Owner));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var librarian in EntityQuery<LibrarianPowersComponent>())
            {
                //Close enough to library to use powers
                if (libraryCoordinates != null && TryComp<TransformComponent>(librarian.Owner, out var xform))
                {
                    bool inDomain = xform.Coordinates.InRange(_entityManager, (EntityCoordinates) libraryCoordinates, librarian.LibraryDomainRange);
                    if(!librarian.Enabled && inDomain)
                        EnterLibraryDomain(librarian);
                    else if (librarian.Enabled && !inDomain)
                        LeaveLibraryDomain(librarian);
                }

                //Remove shushed status
                if(librarian.ShushedEntity != null)
                {
                    if(librarian.Accumulator >= librarian.ShushTime.TotalSeconds)
                    {
                        EntityUid uid = (EntityUid)librarian.ShushedEntity;
                        _entityManager.RemoveComponent<MutedComponent>(uid);
                        _alertsSystem.ClearAlert(uid, AlertType.Muted);
                        _popupSystem.PopupEntity(Loc.GetString("librarian-shush-can-speak"), uid, Filter.Entities(uid));
                        librarian.ShushedEntity = null;
                    }
                    else
                    {
                        librarian.Accumulator += frameTime;
                    }
                }
            }
        }
    }

    public sealed class ShushActionEvent : EntityTargetActionEvent { }
}
