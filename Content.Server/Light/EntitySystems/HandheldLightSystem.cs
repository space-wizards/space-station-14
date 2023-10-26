using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Light.EntitySystems
{
    public sealed class HandheldLightSystem : SharedHandheldLightSystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPointLightSystem _lights = default!;

        // TODO: Ideally you'd be able to subscribe to power stuff to get events at certain percentages.. or something?
        // But for now this will be better anyway.
        private readonly HashSet<HandheldLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandheldLightComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<HandheldLightComponent, ComponentGetState>(OnGetState);

            SubscribeLocalEvent<HandheldLightComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<HandheldLightComponent, ComponentShutdown>(OnShutdown);

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
            UpdateLevel(uid, component);
        }

        private void OnEntRemoved(
            EntityUid uid,
            HandheldLightComponent component,
            EntRemovedFromContainerMessage args)
        {
            // Ditto above
            UpdateLevel(uid, component);
        }

        private void OnGetActions(EntityUid uid, HandheldLightComponent component, GetItemActionsEvent args)
        {
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
        }

        private void OnToggleAction(EntityUid uid, HandheldLightComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            if (component.Activated)
                TurnOff(uid, component);
            else
                TurnOn(args.Performer, uid, component);

            args.Handled = true;
        }

        private void OnGetState(EntityUid uid, HandheldLightComponent component, ref ComponentGetState args)
        {
            args.State = new HandheldLightComponent.HandheldLightComponentState(component.Activated, GetLevel(uid, component));
        }

        private void OnMapInit(EntityUid uid, HandheldLightComponent component, MapInitEvent args)
        {
            _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
            _actions.AddAction(uid, ref component.SelfToggleActionEntity, component.ToggleAction);
        }

        private void OnShutdown(EntityUid uid, HandheldLightComponent component, ComponentShutdown args)
        {
            _actions.RemoveAction(uid, component.ToggleActionEntity);
            _actions.RemoveAction(uid, component.SelfToggleActionEntity);
        }

        private byte? GetLevel(EntityUid uid, HandheldLightComponent component)
        {
            // Curently every single flashlight has the same number of levels for status and that's all it uses the charge for
            // Thus we'll just check if the level changes.

            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
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
            if (args.Handled || !component.ToggleOnInteract)
                return;

            if (ToggleStatus(args.User, uid, component))
                args.Handled = true;
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        public bool ToggleStatus(EntityUid user, EntityUid uid, HandheldLightComponent component)
        {
            return component.Activated ? TurnOff(uid, component) : TurnOn(user, uid, component);
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
                var uid = handheld.Owner;

                if (handheld.Deleted)
                {
                    toRemove.Add(handheld);
                    continue;
                }

                if (Paused(uid)) continue;
                TryUpdate(uid, handheld, frameTime);
            }

            foreach (var light in toRemove)
            {
                _activeLights.Remove(light);
            }
        }

        private void AddToggleLightVerb(EntityUid uid, HandheldLightComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !component.ToggleOnInteract)
                return;

            ActivationVerb verb = new()
            {
                Text = Loc.GetString("verb-common-toggle-light"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
                Act = component.Activated
                    ? () => TurnOff(uid, component)
                    : () => TurnOn(args.User, uid,  component)
            };

            args.Verbs.Add(verb);
        }

        public bool TurnOff(EntityUid uid, HandheldLightComponent component, bool makeNoise = true)
        {
            if (!component.Activated || !_lights.TryGetLight(uid, out var pointLightComponent))
            {
                return false;
            }

            _lights.SetEnabled(uid, false, pointLightComponent);
            SetActivated(uid, false, component, makeNoise);
            component.Level = null;
            _activeLights.Remove(component);
            return true;
        }

        public bool TurnOn(EntityUid user, EntityUid uid, HandheldLightComponent component)
        {
            if (component.Activated || !_lights.TryGetLight(uid, out var pointLightComponent))
            {
                return false;
            }

            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery) &&
                !TryComp(uid, out battery))
            {
                _audio.PlayPvs(_audio.GetSound(component.TurnOnFailSound), uid);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-missing-message"), uid, user);
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (component.Wattage > battery.CurrentCharge)
            {
                _audio.PlayPvs(_audio.GetSound(component.TurnOnFailSound), uid);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-dead-message"), uid, user);
                return false;
            }

            _lights.SetEnabled(uid, true, pointLightComponent);
            SetActivated(uid, true, component, true);
            _activeLights.Add(component);

            return true;
        }

        public void TryUpdate(EntityUid uid, HandheldLightComponent component, float frameTime)
        {
            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery) &&
                !TryComp(uid, out battery))
            {
                TurnOff(uid, component, false);
                return;
            }

            var appearanceComponent = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);

            var fraction = battery.CurrentCharge / battery.MaxCharge;
            if (fraction >= 0.30)
            {
                _appearance.SetData(uid, HandheldLightVisuals.Power, HandheldLightPowerStates.FullPower, appearanceComponent);
            }
            else if (fraction >= 0.10)
            {
                _appearance.SetData(uid, HandheldLightVisuals.Power, HandheldLightPowerStates.LowPower, appearanceComponent);
            }
            else
            {
                _appearance.SetData(uid, HandheldLightVisuals.Power, HandheldLightPowerStates.Dying, appearanceComponent);
            }

            if (component.Activated && !battery.TryUseCharge(component.Wattage * frameTime))
                TurnOff(uid, component, false);

            UpdateLevel(uid, component);
        }

        private void UpdateLevel(EntityUid uid, HandheldLightComponent comp)
        {
            var level = GetLevel(uid, comp);

            if (level == comp.Level)
                return;

            comp.Level = level;
            Dirty(comp);
        }
    }
}
