using System.Collections.Generic;
using System.Linq;
using Content.Server.Clothing.Components;
using Content.Server.Items;
using Content.Server.Light.Components;
using Content.Server.Popups;
using Content.Server.PowerCell.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
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
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

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
            SubscribeLocalEvent<HandheldLightComponent, GetActivationVerbsEvent>(AddToggleLightVerb);
            SubscribeLocalEvent<HandheldLightComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<HandheldLightComponent, UseInHandEvent>(OnUse);
        }

        private void OnGetState(EntityUid uid, HandheldLightComponent component, ref ComponentGetState args)
        {
            args.State = new SharedHandheldLightComponent.HandheldLightComponentState(GetLevel(component));
        }

        private byte? GetLevel(HandheldLightComponent component)
        {
            // Curently every single flashlight has the same number of levels for status and that's all it uses the charge for
            // Thus we'll just check if the level changes.
            if (component.Cell == null)
                return null;

            var currentCharge = component.Cell.CurrentCharge;

            if (MathHelper.CloseToPercent(currentCharge, 0) || component.Wattage > currentCharge)
                return 0;

            return (byte?) ContentHelpers.RoundToNearestLevels(currentCharge / component.Cell.MaxCharge * 255, 255, SharedHandheldLightComponent.StatusLevels);
        }

        private void OnInit(EntityUid uid, HandheldLightComponent component, ComponentInit args)
        {
            EntityManager.EnsureComponent<PointLightComponent>(uid);
            component.CellSlot = EntityManager.EnsureComponent<PowerCellSlotComponent>(uid);

            // Want to make sure client has latest data on level so battery displays properly.
            component.Dirty(EntityManager);
        }

        private void OnRemove(EntityUid uid, HandheldLightComponent component, ComponentRemove args)
        {
            _activeLights.Remove(component);
        }

        private void OnInteractUsing(EntityUid uid, HandheldLightComponent component, InteractUsingEvent args)
        {
            // TODO: https://github.com/space-wizards/space-station-14/pull/5864#discussion_r775191916
            if (args.Handled) return;

            if (!_blocker.CanInteract(args.User)) return;
            if (!component.CellSlot.InsertCell(args.Used)) return;
            component.Dirty(EntityManager);
            args.Handled = true;
        }

        private void OnUse(EntityUid uid, HandheldLightComponent component, UseInHandEvent args)
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
            if (!_blocker.CanUse(user)) return false;
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

                if (handheld.Paused) continue;
                TryUpdate(handheld, frameTime);
            }

            foreach (var light in toRemove)
            {
                _activeLights.Remove(light);
            }
        }

        private void AddToggleLightVerb(EntityUid uid, HandheldLightComponent component, GetActivationVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract) return;

            Verb verb = new()
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

            if (component.Cell == null)
            {
                SoundSystem.Play(Filter.Pvs(component.Owner), component.TurnOnFailSound.GetSound(), component.Owner);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-missing-message"), component.Owner, Filter.Entities(user));
                UpdateLightAction(component);
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (component.Wattage > component.Cell.CurrentCharge)
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

            if (EntityManager.TryGetComponent(component.Owner, out ClothingComponent? clothing))
            {
                clothing.ClothingEquippedPrefix = Loc.GetString(on ? "on" : "off");
            }

            if (EntityManager.TryGetComponent(component.Owner, out ItemComponent? item))
            {
                item.EquippedPrefix = Loc.GetString(on ? "on" : "off");
            }
        }

        private void UpdateLightAction(HandheldLightComponent component)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out ItemActionsComponent? actions)) return;

            actions.Toggle(ItemActionType.ToggleLight, component.Activated);
        }

        public void TryUpdate(HandheldLightComponent component, float frameTime)
        {
            if (component.Cell == null)
            {
                TurnOff(component, false);
                return;
            }

            var appearanceComponent = EntityManager.GetComponent<AppearanceComponent>(component.Owner);

            if (component.Cell.MaxCharge - component.Cell.CurrentCharge < component.Cell.MaxCharge * 0.70)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.FullPower);
            }
            else if (component.Cell.MaxCharge - component.Cell.CurrentCharge < component.Cell.MaxCharge * 0.90)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.LowPower);
            }
            else
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.Dying);
            }

            if (component.Activated && !component.Cell.TryUseCharge(component.Wattage * frameTime)) TurnOff(component, false);

            var level = GetLevel(component);

            if (level != component.LastLevel)
            {
                component.LastLevel = level;
                component.Dirty(EntityManager);
            }
        }
    }
}
