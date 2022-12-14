using Content.Shared.Examine;
using Content.Shared.PAI;
using Content.Shared.Verbs;
using Content.Server.Popups;
using Content.Server.Instruments;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Server.PAI
{
    public sealed class PAISystem : SharedPAISystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InstrumentSystem _instrumentSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PAIComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<PAIComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PAIComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<PAIComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<PAIComponent, GetVerbsEvent<ActivationVerb>>(AddWipeVerb);
        }

        private void OnExamined(EntityUid uid, PAIComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (EntityManager.TryGetComponent<MindComponent>(uid, out var mind) && mind.HasMind)
                {
                    args.PushMarkup(Loc.GetString("pai-system-pai-installed"));
                }
                else if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
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
            if (EntityManager.TryGetComponent<MindComponent>(uid, out var mind) && mind.HasMind)
            {
                _popupSystem.PopupEntity(Loc.GetString("pai-system-pai-installed"), uid, args.User, PopupType.Large);
                return;
            }
            else if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("pai-system-still-searching"), uid, args.User);
                return;
            }

            // Ownership tag
            string val = Loc.GetString("pai-system-pai-name", ("owner", args.User));

            // TODO Identity? People shouldn't dox-themselves by carrying around a PAI.
            // But having the pda's name permanently be "old lady's PAI" is weird.
            // Changing the PAI's identity in a way that ties it to the owner's identity also seems weird.
            // Cause then you could remotely figure out information about the owner's equipped items.

            EntityManager.GetComponent<MetaDataComponent>(component.Owner).EntityName = val;

            var ghostFinder = EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

            ghostFinder.RoleName = Loc.GetString("pai-system-role-name");
            ghostFinder.RoleDescription = Loc.GetString("pai-system-role-description");
            ghostFinder.WhitelistRequired = false;

            _popupSystem.PopupEntity(Loc.GetString("pai-system-searching"), uid, args.User);
            UpdatePAIAppearance(uid, PAIStatus.Searching);
        }

        private void OnMindRemoved(EntityUid uid, PAIComponent component, MindRemovedMessage args)
        {
            // Mind was removed, shutdown the PAI.
            PAITurningOff(uid);
        }

        private void OnMindAdded(EntityUid uid, PAIComponent pai, MindAddedMessage args)
        {
            // Mind was added, shutdown the ghost role stuff so it won't get in the way
            if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
                EntityManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);
            UpdatePAIAppearance(uid, PAIStatus.On);
        }

        private void PAITurningOff(EntityUid uid)
        {
            UpdatePAIAppearance(uid, PAIStatus.Off);

            //  Close the instrument interface if it was open
            //  before closing
            if (HasComp<ActiveInstrumentComponent>(uid) && TryComp<ActorComponent>(uid, out var actor))
            {
                _instrumentSystem.ToggleInstrumentUi(uid, actor.PlayerSession);
            }

            //  Stop instrument
            if (EntityManager.TryGetComponent<InstrumentComponent>(uid, out var instrument)) _instrumentSystem.Clean(uid, instrument);
            if (EntityManager.TryGetComponent<MetaDataComponent>(uid, out var metadata))
            {
                var proto = metadata.EntityPrototype;
                if (proto != null)
                    metadata.EntityName = proto.Name;
            }
        }

        private void UpdatePAIAppearance(EntityUid uid, PAIStatus status)
        {
            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
            {
                appearance.SetData(PAIVisuals.Status, status);
            }
        }

        private void AddWipeVerb(EntityUid uid, PAIComponent pai, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (EntityManager.TryGetComponent<MindComponent>(uid, out var mind) && mind.HasMind)
            {
                ActivationVerb verb = new();
                verb.Text = Loc.GetString("pai-system-wipe-device-verb-text");
                verb.Act = () => {
                    if (pai.Deleted)
                        return;
                    // Wiping device :(
                    // The shutdown of the Mind should cause automatic reset of the pAI during OnMindRemoved
                    // EDIT: But it doesn't!!!! Wtf? Do stuff manually
                    if (EntityManager.HasComponent<MindComponent>(uid))
                    {
                        EntityManager.RemoveComponent<MindComponent>(uid);
                        _popupSystem.PopupEntity(Loc.GetString("pai-system-wiped-device"), uid, args.User, PopupType.Large);
                        PAITurningOff(uid);
                    }
                };
                args.Verbs.Add(verb);
            }
            else if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
            {
                ActivationVerb verb = new();
                verb.Text = Loc.GetString("pai-system-stop-searching-verb-text");
                verb.Act = () => {
                    if (pai.Deleted)
                        return;
                    if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
                    {
                        EntityManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);
                        _popupSystem.PopupEntity(Loc.GetString("pai-system-stopped-searching"), uid, args.User);
                        PAITurningOff(uid);
                    }
                };
                args.Verbs.Add(verb);
            }
        }
    }
}
