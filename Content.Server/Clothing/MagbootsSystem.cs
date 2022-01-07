using Content.Server.Alert;
using Content.Server.Atmos.Components;
using Content.Server.Clothing.Components;
using Content.Shared.Alert;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Slippery;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Clothing
{
    public sealed class MagbootsSystem : EntitySystem
    {
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MagbootsComponent, GetActivationVerbsEvent>(AddToggleVerb);
            SubscribeLocalEvent<MagbootsComponent, SlipAttemptEvent>(OnSlipAttempt);
            SubscribeLocalEvent<MagbootsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
            SubscribeLocalEvent<MagbootsComponent, GotEquippedEvent>(OnGotEquipped);
            SubscribeLocalEvent<MagbootsComponent, GotUnequippedEvent>(OnGotUnequipped);
        }

        public void UpdateMagbootEffects(EntityUid parent, EntityUid uid, bool state, MagbootsComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;
            state = state && component.On;

            if (TryComp(parent, out MovedByPressureComponent? movedByPressure))
            {
                movedByPressure.Enabled = state;
            }

            if (state)
            {
                _alertsSystem.ShowAlert(parent, AlertType.Magboots);
            }
            else
            {
                _alertsSystem.ClearAlert(parent, AlertType.Magboots);
            }
        }

        private void OnGotUnequipped(EntityUid uid, MagbootsComponent component, GotUnequippedEvent args)
        {
            if (args.Slot == "shoes")
            {
                UpdateMagbootEffects(args.Equipee, uid, false, component);
            }
        }

        private void OnGotEquipped(EntityUid uid, MagbootsComponent component, GotEquippedEvent args)
        {
            if (args.Slot == "shoes")
            {
                UpdateMagbootEffects(args.Equipee, uid, true, component);
            }
        }

        private void OnRefreshMovespeed(EntityUid uid, MagbootsComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }

        private void AddToggleVerb(EntityUid uid, MagbootsComponent component, GetActivationVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("toggle-magboots-verb-get-data-text");
            verb.Act = () => component.On = !component.On;
            // TODO VERB ICON add toggle icon? maybe a computer on/off symbol?
            args.Verbs.Add(verb);
        }

        private void OnSlipAttempt(EntityUid uid, MagbootsComponent component, SlipAttemptEvent args)
        {
            if (component.On)
            {
                args.Cancel();
            }
        }
    }
}
