using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.PAI;
using Content.Shared.Verbs;
using Content.Server.Popups;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Robust.Server.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
            SubscribeLocalEvent<PAIComponent, GetActivationVerbsEvent>(AddWipeVerb);
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
            UpdatePAIAppearance(component, PAIStatus.Searching);
        }

        private void OnMindRemoved(EntityUid uid, PAIComponent component, MindRemovedMessage args)
        {
            // Mind was removed, shutdown the PAI.
            UpdatePAIAppearance(component, PAIStatus.Off);
        }

        private void OnMindAdded(EntityUid uid, PAIComponent pai, MindAddedMessage args)
        {
            // Mind was added, shutdown the ghost role stuff so it won't get in the way
            if (pai.Owner.HasComponent<GhostTakeoverAvailableComponent>())
                pai.Owner.RemoveComponent<GhostTakeoverAvailableComponent>();
            UpdatePAIAppearance(pai, PAIStatus.On);
        }

        private void UpdatePAIAppearance(PAIComponent pai, PAIStatus status)
        {
            if (pai.Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                appearance.SetData(PAIVisuals.Status, status);
            }
        }

        private void AddWipeVerb(EntityUid uid, PAIComponent pai, GetActivationVerbsEvent args)
        {
            if (args.User == null || !args.CanAccess || !args.CanInteract)
                return;

            if (!(pai.Owner.TryGetComponent<MindComponent>(out var mind) && mind.HasMind))
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("pai-system-wipe-device-verb-text");
            verb.Act = () => {
                // Wiping device :(
                // The shutdown of the Mind should cause automatic reset of the pAI during OnMindRemoved
                // EDIT: But it doesn't!!!! Wtf? Do stuff manually
                if (pai.Owner.HasComponent<MindComponent>())
                {
                    pai.Owner.RemoveComponent<MindComponent>();
                    _popupSystem.PopupEntity(Loc.GetString("pai-system-wiped-device"), uid, Filter.Entities(args.User.Uid));
                    UpdatePAIAppearance(pai, PAIStatus.Off);
                }
            };
            args.Verbs.Add(verb);
        }
    }
}
