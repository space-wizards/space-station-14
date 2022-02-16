using Content.Server.Light.Components;
using Content.Server.Light.Events;
using Content.Shared.Light;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using System;
using Robust.Shared.IoC;

namespace Content.Server.Light.EntitySystems
{
    public sealed class UnpoweredFlashlightSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UnpoweredFlashlightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerbs);
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
        }

    }
}
