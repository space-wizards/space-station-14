using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class HandheldLightSystem : SharedHandheldLightSystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        // TODO: Ideally you'd be able to subscribe to power stuff to get events at certain percentages.. or something?
        // But for now this will be better anyway.
        private readonly HashSet<HandheldLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandheldLightComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<HandheldLightComponent, ComponentGetState>(OnGetState);

            SubscribeLocalEvent<HandheldLightComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<HandheldLightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerb);

            SubscribeLocalEvent<HandheldLightComponent, ActivateInWorldEvent>(OnActivate);

            SubscribeLocalEvent<HandheldLightComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<HandheldLightComponent, ToggleActionEvent>(OnToggleAction);

            SubscribeLocalEvent<HandheldLightComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<HandheldLightComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        private void OnEntInserted(
            EntityUid uid,
            HandheldLightComponent component,
            EntInsertedIntoContainerMessage args)
        {
            // Not guaranteed to be the correct container for our slot, I don't care.
            UpdateLevel(component);
        }

        private void OnEntRemoved(
            EntityUid uid,
            HandheldLightComponent component,
            EntRemovedFromContainerMessage args)
        {
            // Ditto above
            UpdateLevel(component);
        }

        private void OnGetActions(EntityUid uid, HandheldLightComponent component, GetItemActionsEvent args)
        {
            if (component.ToggleAction == null
                && _proto.TryIndex(component.ToggleActionId, out InstantActionPrototype? act))
            {
                component.ToggleAction = new(act);
            }

            if (component.ToggleAction != null)
                args.Actions.Add(component.ToggleAction);
        }

        private void OnToggleAction(EntityUid uid, HandheldLightComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            if (component.Activated)
                TurnOff(component);
            else
                TurnOn(args.Performer, component);

            args.Handled = true;
        }

        private void OnGetState(EntityUid uid, HandheldLightComponent component, ref ComponentGetState args)
        {
            args.State = new HandheldLightComponent.HandheldLightComponentState(component.Activated, GetLevel(component));
        }

        private byte? GetLevel(HandheldLightComponent component)
        {
            // Curently every single flashlight has the same number of levels for status and that's all it uses the charge for
            // Thus we'll just check if the level changes.

            if (!_powerCell.TryGetBatteryFromSlot(component.Owner, out var battery))
                return null;

            if (MathHelper.CloseToPercent(battery.CurrentCharge, 0) || component.Wattage > battery.CurrentCharge)
                return 0;

            return (byte?) ContentHelpers.RoundToNearestLevels(battery.CurrentCharge / battery.MaxCharge * 255, 255, HandheldLightComponent.StatusLevels);
        }

        private void OnRemove(EntityUid uid, HandheldLightComponent component, ComponentRemove args)
        {
            _activeLights.Remove(component);
        }

        private void OnActivate(EntityUid uid, HandheldLightComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled) return;

            if (ToggleStatus(args.User, component))
                args.Handled = true;
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        public bool ToggleStatus(EntityUid user, HandheldLightComponent component)
        {
            return component.Activated ? TurnOff(component) : TurnOn(user, component);
        }

        private void OnExamine(EntityUid uid, HandheldLightComponent component, ExaminedEvent args)
        {
            args.PushMarkup(component.Activated
                ? Loc.GetString("handheld-light-component-on-examine-is-on-message")
                : Loc.GetString("handheld-light-component-on-examine-is-off-message"));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _activeLights.Clear();
        }

        public override void Update(float frameTime)
        {
            var toRemove = new RemQueue<HandheldLightComponent>();

            foreach (var handheld in _activeLights)
            {
                if (handheld.Deleted)
                {
                    toRemove.Add(handheld);
                    continue;
                }

                if (Paused(handheld.Owner)) continue;
                TryUpdate(handheld, frameTime);
            }

            foreach (var light in toRemove)
            {
                _activeLights.Remove(light);
            }
        }

        private void AddToggleLightVerb(EntityUid uid, HandheldLightComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract) return;

            ActivationVerb verb = new()
            {
                Text = Loc.GetString("verb-common-toggle-light"),
                IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png",
                Act = component.Activated
                    ? () => TurnOff(component)
                    : () => TurnOn(args.User, component)
            };

            args.Verbs.Add(verb);
        }

        public bool TurnOff(HandheldLightComponent component, bool makeNoise = true)
        {
            if (!component.Activated || !TryComp<PointLightComponent>(component.Owner, out var pointLightComponent))
            {
                return false;
            }

            pointLightComponent.Enabled = false;
            SetActivated(component.Owner, false, component, makeNoise);
            component.Level = null;
            _activeLights.Remove(component);
            return true;
        }

        public bool TurnOn(EntityUid user, HandheldLightComponent component)
        {
            if (component.Activated || !TryComp<PointLightComponent>(component.Owner, out var pointLightComponent))
            {
                return false;
            }

            if (!_powerCell.TryGetBatteryFromSlot(component.Owner, out var battery) &&
                !TryComp(component.Owner, out battery))
            {
                _audio.PlayPvs(_audio.GetSound(component.TurnOnFailSound), component.Owner);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-missing-message"), component.Owner, user);
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (component.Wattage > battery.CurrentCharge)
            {
                _audio.PlayPvs(_audio.GetSound(component.TurnOnFailSound), component.Owner);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-dead-message"), component.Owner, user);
                return false;
            }

            pointLightComponent.Enabled = true;
            SetActivated(component.Owner, true, component, true);
            _activeLights.Add(component);

            return true;
        }

        public void TryUpdate(HandheldLightComponent component, float frameTime)
        {
            if (!_powerCell.TryGetBatteryFromSlot(component.Owner, out var battery) &&
                !TryComp(component.Owner, out battery))
            {
                TurnOff(component, false);
                return;
            }

            var appearanceComponent = EntityManager.GetComponent<AppearanceComponent>(component.Owner);

            var fraction = battery.CurrentCharge / battery.MaxCharge;
            if (fraction >= 0.30)
            {
                _appearance.SetData(component.Owner, HandheldLightVisuals.Power, HandheldLightPowerStates.FullPower, appearanceComponent);
            }
            else if (fraction >= 0.10)
            {
                _appearance.SetData(component.Owner, HandheldLightVisuals.Power, HandheldLightPowerStates.LowPower, appearanceComponent);
            }
            else
            {
                _appearance.SetData(component.Owner, HandheldLightVisuals.Power, HandheldLightPowerStates.Dying, appearanceComponent);
            }

            if (component.Activated && !battery.TryUseCharge(component.Wattage * frameTime))
                TurnOff(component, false);

            UpdateLevel(component);
        }

        private void UpdateLevel(HandheldLightComponent comp)
        {
            var level = GetLevel(comp);

            if (level == comp.Level)
                return;

            comp.Level = level;
            Dirty(comp);
        }
    }
}
