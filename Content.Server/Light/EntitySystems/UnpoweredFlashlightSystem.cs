using Content.Server.Light.Components;
using Content.Server.Light.Events;
using Content.Shared.Actions;
using Content.Shared.Light;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    public sealed class UnpoweredFlashlightSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UnpoweredFlashlightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerbs);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, GetActionsEvent>(OnGetActions);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, ToggleActionEvent>(OnToggleAction);
        }

        private void OnToggleAction(EntityUid uid, UnpoweredFlashlightComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            ToggleLight(component);

            args.Handled = true;
        }

        private void OnGetActions(EntityUid uid, UnpoweredFlashlightComponent component, GetActionsEvent args)
        {
            args.Actions.Add(component.ToggleAction);
        }

        private void AddToggleLightVerbs(EntityUid uid, UnpoweredFlashlightComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            ActivationVerb verb = new();
            verb.Text = Loc.GetString("toggle-flashlight-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            verb.Act = () => ToggleLight(component);
            verb.Priority = -1; // For things like PDA's, Open-UI and other verbs that should be higher priority.

            args.Verbs.Add(verb);
        }

        public void ToggleLight(UnpoweredFlashlightComponent flashlight)
        {
            if (!EntityManager.TryGetComponent(flashlight.Owner, out PointLightComponent? light))
                return;

            flashlight.LightOn = !flashlight.LightOn;
            light.Enabled = flashlight.LightOn;

            if (EntityManager.TryGetComponent(flashlight.Owner, out AppearanceComponent? appearance))
                appearance.SetData(UnpoweredFlashlightVisuals.LightOn, flashlight.LightOn);

            SoundSystem.Play(Filter.Pvs(light.Owner), flashlight.ToggleSound.GetSound(), flashlight.Owner);

            RaiseLocalEvent(flashlight.Owner, new LightToggleEvent(flashlight.LightOn));
            _actionsSystem.SetToggled(flashlight.ToggleAction, flashlight.LightOn);
        }
    }
}
