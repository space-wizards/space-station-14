using System.Collections.Generic;
using System.Linq;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.PowerCell.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandHeldLightSystem : EntitySystem
    {
        // TODO: Ideally you'd be able to subscribe to power stuff to get events at certain percentages.. or something?
        // But for now this will be better anyway.
        private HashSet<HandheldLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActivateHandheldLightMessage>(HandleActivate);
            SubscribeLocalEvent<DeactivateHandheldLightMessage>(HandleDeactivate);
            SubscribeLocalEvent<HandheldLightComponent, GetActivationVerbsEvent>(AddToggleLightVerb);
            SubscribeLocalEvent<ChargerInsertEvent>(OnChargerInsert);
            SubscribeLocalEvent<ChargerEjectEvent>(OnChargerEject);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _activeLights.Clear();
        }

        private void HandleActivate(ActivateHandheldLightMessage message)
        {
            _activeLights.Add(message.Component);
        }

        private void HandleDeactivate(DeactivateHandheldLightMessage message)
        {
            _activeLights.Remove(message.Component);
        }

        public override void Update(float frameTime)
        {
            foreach (var handheld in _activeLights.ToArray())
            {
                if (handheld.Deleted || handheld.Paused) continue;
                handheld.OnUpdate(frameTime);
            }
        }

        private void AddToggleLightVerb(EntityUid uid, HandheldLightComponent component, GetActivationVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("verb-common-toggle-light");
            verb.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            verb.Act = component.Activated
                ? () => component.TurnOff()
                : () => component.TurnOn(args.User);

            args.Verbs.Add(verb);
        }

        /// <summary>
        ///     When a HandheldLight is inserted into a charger, turn it off so it does not constantly drain power and never finish charging.
        /// </summary>
        private void OnChargerInsert(ChargerInsertEvent args)
        {
            if (args.InsertedEntity.TryGetComponent(out HandheldLightComponent? light))
            {
                light.TurnOff(makeNoise: false);
            }
            return;
        }

        /// <summary>
        ///     When a HandheldLight is ejected from a charger, we need to send the final charge level and current state of the light.
        /// </summary>
        private void OnChargerEject(ChargerEjectEvent args)
        {
            if (args.EjectedEntity.TryGetComponent(out HandheldLightComponent? light))
            {
                light.Dirty();
            }
            return;
        }
    }
}
