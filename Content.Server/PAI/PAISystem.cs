using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.PAI
{
    public class PAISystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PAIComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<PAIComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PAIComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<PAIComponent, MindRemovedMessage>(OnMindRemoved);
        }

        private void OnExamined(EntityUid uid, PAIComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (component.Owner.TryGetComponent<MindComponent>(out var mind) && mind.HasMind)
                {
                    args.PushMarkup(Loc.GetString("pai-system-pai-installed"));
                }
                else if (component.Owner.HasComponent<GhostTakeoverAvailableComponent>())
                {
                    args.PushMarkup(Loc.GetString("pai-system-still-searching"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("pai-system-off"));
                }
            }
        }

        private void OnUseInHand(EntityUid uid, PAIComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            // Placeholder PAIs are essentially portable ghost role generators.

            args.Handled = true;

            // Check for pAI activation
            if (component.Owner.TryGetComponent<MindComponent>(out var mind) && mind.HasMind)
            {
                _popupSystem.PopupEntity(Loc.GetString("pai-system-pai-installed"), uid, Filter.Entities(args.User.Uid));
                return;
            }
            else if (component.Owner.HasComponent<GhostTakeoverAvailableComponent>())
            {
                _popupSystem.PopupEntity(Loc.GetString("pai-system-still-searching"), uid, Filter.Entities(args.User.Uid));
                return;
            }

            var ghostFinder = component.Owner.EnsureComponent<GhostTakeoverAvailableComponent>();

            ghostFinder.RoleName = Loc.GetString("pai-system-role-name");
            ghostFinder.RoleDescription = Loc.GetString("pai-system-role-description");

            _popupSystem.PopupEntity(Loc.GetString("pai-system-searching"), uid, Filter.Entities(args.User.Uid));
        }

        private void OnMindRemoved(EntityUid uid, PAIComponent component, MindRemovedMessage args)
        {
            // Mind was removed, shutdown the PAI.
            if (component.Owner.HasComponent<GhostTakeoverAvailableComponent>())
                component.Owner.RemoveComponent<GhostTakeoverAvailableComponent>();
        }

        private void OnMindAdded(EntityUid uid, PAIComponent pai, MindAddedMessage args)
        {
            // Mind was added. Moony, please go annoy the player somehow.
        }
    }
}
