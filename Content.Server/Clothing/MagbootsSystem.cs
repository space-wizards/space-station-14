using Content.Server.Clothing.Components;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Slippery;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Clothing
{
    public sealed class MagbootsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MagbootsComponent, GetActivationVerbsEvent>(AddToggleVerb);
            SubscribeLocalEvent<MagbootsComponent, SlipAttemptEvent>(OnSlipAttempt);
            SubscribeLocalEvent<MagbootsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, MagbootsComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
        }

        private void AddToggleVerb(EntityUid uid, MagbootsComponent component, GetActivationVerbsEvent args)
        {
            if (args.User == null || !args.CanAccess || !args.CanInteract)
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
