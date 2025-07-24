using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Server.Light.EntitySystems
{
    public sealed class HandheldLightSystem : SharedHandheldLightSystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPointLightSystem _lights = default!;

        // TODO: Ideally you'd be able to subscribe to power stuff to get events at certain percentages.. or something?
        // But for now this will be better anyway.
        private readonly HashSet<Entity<HandheldLightComponent>> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandheldLightComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<HandheldLightComponent, ComponentGetState>(OnGetState);

            SubscribeLocalEvent<HandheldLightComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<HandheldLightComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<HandheldLightComponent, ExaminedEvent>(OnExamine);

            SubscribeLocalEvent<HandheldLightComponent, ActivateInWorldEvent>(OnActivate);

            SubscribeLocalEvent<HandheldLightComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<HandheldLightComponent, ToggleActionEvent>(OnToggleAction);

            SubscribeLocalEvent<HandheldLightComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<HandheldLightComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        private void OnEntInserted(Entity<HandheldLightComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            // Not guaranteed to be the correct container for our slot, I don't care.
            UpdateLevel(ent);
        }

        private void OnEntRemoved(Entity<HandheldLightComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            // Ditto above
            UpdateLevel(ent);
        }

        private void OnGetActions(EntityUid uid, HandheldLightComponent component, GetItemActionsEvent args)
        {
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
        }

        private void OnToggleAction(Entity<HandheldLightComponent> ent, ref ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            if (ent.Comp.Activated)
                TurnOff(ent);
            else
                TurnOn(args.Performer, ent);

            args.Handled = true;
        }

        private void OnGetState(Entity<HandheldLightComponent> ent, ref ComponentGetState args)
        {
            args.State = new HandheldLightComponent.HandheldLightComponentState(ent.Comp.Activated, GetLevel(ent));
        }

        private void OnMapInit(Entity<HandheldLightComponent> ent, ref MapInitEvent args)
        {
            var component = ent.Comp;
            _actionContainer.EnsureAction(ent, ref component.ToggleActionEntity, component.ToggleAction);
            _actions.AddAction(ent, ref component.SelfToggleActionEntity, component.ToggleAction);
        }

        private void OnShutdown(EntityUid uid, HandheldLightComponent component, ComponentShutdown args)
        {
            _actions.RemoveAction(uid, component.ToggleActionEntity);
            _actions.RemoveAction(uid, component.SelfToggleActionEntity);
        }

        private byte? GetLevel(Entity<HandheldLightComponent> ent)
        {
            // Curently every single flashlight has the same number of levels for status and that's all it uses the charge for
            // Thus we'll just check if the level changes.

            if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery))
                return null;

            if (MathHelper.CloseToPercent(battery.CurrentCharge, 0) || ent.Comp.Wattage > battery.CurrentCharge)
                return 0;

            return (byte?) ContentHelpers.RoundToNearestLevels(battery.CurrentCharge / battery.MaxCharge * 255, 255, HandheldLightComponent.StatusLevels);
        }

        private void OnRemove(Entity<HandheldLightComponent> ent, ref ComponentRemove args)
        {
            _activeLights.Remove(ent);
        }

        private void OnActivate(Entity<HandheldLightComponent> ent, ref ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex || !ent.Comp.ToggleOnInteract)
                return;

            if (ToggleStatus(args.User, ent))
                args.Handled = true;
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        public bool ToggleStatus(EntityUid user, Entity<HandheldLightComponent> ent)
        {
            return ent.Comp.Activated ? TurnOff(ent) : TurnOn(user, ent);
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
            var toRemove = new RemQueue<Entity<HandheldLightComponent>>();

            foreach (var handheld in _activeLights)
            {
                if (handheld.Comp.Deleted)
                {
                    toRemove.Add(handheld);
                    continue;
                }

                if (Paused(handheld))
                    continue;

                TryUpdate(handheld, frameTime);
            }

            foreach (var light in toRemove)
            {
                _activeLights.Remove(light);
            }
        }

        public override bool TurnOff(Entity<HandheldLightComponent> ent, bool makeNoise = true)
        {
            if (!ent.Comp.Activated || !_lights.TryGetLight(ent, out var pointLightComponent))
            {
                return false;
            }

            _lights.SetEnabled(ent, false, pointLightComponent);
            SetActivated(ent, false, ent, makeNoise);
            ent.Comp.Level = null;
            _activeLights.Remove(ent);
            return true;
        }

        public override bool TurnOn(EntityUid user, Entity<HandheldLightComponent> uid)
        {
            var component = uid.Comp;
            if (component.Activated || !_lights.TryGetLight(uid, out var pointLightComponent))
            {
                return false;
            }

            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery) &&
                !TryComp(uid, out battery))
            {
                _audio.PlayPvs(_audio.ResolveSound(component.TurnOnFailSound), uid);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-missing-message"), uid, user);
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (component.Wattage > battery.CurrentCharge)
            {
                _audio.PlayPvs(_audio.ResolveSound(component.TurnOnFailSound), uid);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-dead-message"), uid, user);
                return false;
            }

            _lights.SetEnabled(uid, true, pointLightComponent);
            SetActivated(uid, true, component, true);
            _activeLights.Add(uid);

            return true;
        }

        public void TryUpdate(Entity<HandheldLightComponent> uid, float frameTime)
        {
            var component = uid.Comp;
            if (!_powerCell.TryGetBatteryFromSlot(uid, out var batteryUid, out var battery, null) &&
                !TryComp(uid, out battery))
            {
                TurnOff(uid, false);
                return;
            }

            if (batteryUid == null)
                return;

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

            if (component.Activated && !_battery.TryUseCharge(batteryUid.Value, component.Wattage * frameTime, battery))
                TurnOff(uid, false);

            UpdateLevel(uid);
        }

        private void UpdateLevel(Entity<HandheldLightComponent> ent)
        {
            var level = GetLevel(ent);

            if (level == ent.Comp.Level)
                return;

            ent.Comp.Level = level;
            Dirty(ent);
        }
    }
}
