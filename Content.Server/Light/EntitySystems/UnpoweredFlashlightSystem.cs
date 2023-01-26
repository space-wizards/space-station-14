using Content.Server.Light.Components;
using Content.Server.Light.Events;
using Content.Server.Mind.Components;
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
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UnpoweredFlashlightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerbs);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, ToggleActionEvent>(OnToggleAction);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, MindAddedMessage>(OnMindAdded);
        }

        private void OnToggleAction(EntityUid uid, UnpoweredFlashlightComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            ToggleLight(uid, component);

            args.Handled = true;
        }

        private void OnGetActions(EntityUid uid, UnpoweredFlashlightComponent component, GetItemActionsEvent args)
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
            verb.Act = () => ToggleLight(uid, component);
            verb.Priority = -1; // For things like PDA's, Open-UI and other verbs that should be higher priority.

            args.Verbs.Add(verb);
        }

        private void OnMindAdded(EntityUid uid, UnpoweredFlashlightComponent component, MindAddedMessage args)
        {
            _actionsSystem.AddAction(uid, component.ToggleAction, null);
        }
        public void ToggleLight(EntityUid uid, UnpoweredFlashlightComponent flashlight)
        {
            if (!EntityManager.TryGetComponent(flashlight.Owner, out PointLightComponent? light))
                return;

            flashlight.LightOn = !flashlight.LightOn;
            light.Enabled = flashlight.LightOn;

            if (EntityManager.TryGetComponent(flashlight.Owner, out AppearanceComponent? appearance))
                _appearance.SetData(uid, UnpoweredFlashlightVisuals.LightOn, flashlight.LightOn, appearance);

            SoundSystem.Play(flashlight.ToggleSound.GetSound(), Filter.Pvs(light.Owner), flashlight.Owner);

            RaiseLocalEvent(flashlight.Owner, new LightToggleEvent(flashlight.LightOn), true);
            _actionsSystem.SetToggled(flashlight.ToggleAction, flashlight.LightOn);
        }
    }
}
