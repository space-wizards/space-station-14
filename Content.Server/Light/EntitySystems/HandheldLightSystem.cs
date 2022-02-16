using System.Collections.Generic;
using Content.Server.Clothing.Components;
using Content.Server.Light.Components;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Light.Component;
using Content.Shared.Rounding;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class HandheldLightSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;

        // TODO: Ideally you'd be able to subscribe to power stuff to get events at certain percentages.. or something?
        // But for now this will be better anyway.
        private readonly HashSet<HandheldLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandheldLightComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<HandheldLightComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<HandheldLightComponent, ComponentGetState>(OnGetState);

            SubscribeLocalEvent<HandheldLightComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<HandheldLightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerb);

            SubscribeLocalEvent<HandheldLightComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnGetState(EntityUid uid, HandheldLightComponent component, ref ComponentGetState args)
        {
            args.State = new SharedHandheldLightComponent.HandheldLightComponentState(GetLevel(component));
        }

        private byte? GetLevel(HandheldLightComponent component)
        {
            // Curently every single flashlight has the same number of levels for status and that's all it uses the charge for
            // Thus we'll just check if the level changes.

            if (!_powerCell.TryGetBatteryFromSlot(component.Owner, out var battery))
                return null;

            if (MathHelper.CloseToPercent(battery.CurrentCharge, 0) || component.Wattage > battery.CurrentCharge)
                return 0;

            return (byte?) ContentHelpers.RoundToNearestLevels(battery.CurrentCharge / battery.MaxCharge * 255, 255, SharedHandheldLightComponent.StatusLevels);
        }

        private void OnInit(EntityUid uid, HandheldLightComponent component, ComponentInit args)
        {
            EntityManager.EnsureComponent<PointLightComponent>(uid);

            // Want to make sure client has latest data on level so battery displays properly.
            component.Dirty(EntityManager);
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
            if (!component.Activated) return false;

            SetState(component, false);
            component.Activated = false;
            UpdateLightAction(component);
            _activeLights.Remove(component);
            component.LastLevel = null;
            component.Dirty(EntityManager);

            if (makeNoise)
                SoundSystem.Play(Filter.Pvs(component.Owner), component.TurnOffSound.GetSound(), component.Owner);

            return true;
        }

        public bool TurnOn(EntityUid user, HandheldLightComponent component)
        {
            if (component.Activated) return false;

            if (!_powerCell.TryGetBatteryFromSlot(component.Owner, out var battery))
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), component.TurnOnFailSound.GetSound(), component.Owner);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-missing-message"), component.Owner, Filter.Entities(user));
                UpdateLightAction(component);
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (component.Wattage > battery.CurrentCharge)
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), component.TurnOnFailSound.GetSound(), component.Owner);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-dead-message"), component.Owner, Filter.Entities(user));
                UpdateLightAction(component);
                return false;
            }

            component.Activated = true;
            UpdateLightAction(component);
            SetState(component, true);
            _activeLights.Add(component);
            component.LastLevel = GetLevel(component);
            component.Dirty(EntityManager);

            SoundSystem.Play(Filter.Pvs(component.Owner), component.TurnOnSound.GetSound(), component.Owner);
            return true;
        }

        private void SetState(HandheldLightComponent component, bool on)
        {
            // TODO: Oh dear
            if (EntityManager.TryGetComponent(component.Owner, out SpriteComponent? sprite))
            {
                sprite.LayerSetVisible(1, on);
            }

            if (EntityManager.TryGetComponent(component.Owner, out PointLightComponent? light))
            {
                light.Enabled = on;
            }

            if (EntityManager.TryGetComponent(component.Owner, out SharedItemComponent? item))
            {
                item.EquippedPrefix = on ? "on" : "off";
            }
        }

        private void UpdateLightAction(HandheldLightComponent component)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out ItemActionsComponent? actions)) return;

            actions.Toggle(ItemActionType.ToggleLight, component.Activated);
        }

        public void TryUpdate(HandheldLightComponent component, float frameTime)
        {
            if (!_powerCell.TryGetBatteryFromSlot(component.Owner, out var battery))
            {
                TurnOff(component, false);
                return;
            }

            var appearanceComponent = EntityManager.GetComponent<AppearanceComponent>(component.Owner);

            var fraction = battery.CurrentCharge / battery.MaxCharge;
            if (fraction >= 0.30)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.FullPower);
            }
            else if (fraction >= 0.10)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.LowPower);
            }
            else
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.Dying);
            }

            if (component.Activated && !battery.TryUseCharge(component.Wattage * frameTime))
                TurnOff(component, false);

            var level = GetLevel(component);

            if (level != component.LastLevel)
            {
                component.LastLevel = level;
                component.Dirty(EntityManager);
            }
        }
    }
}
