using Content.Server.Light.Components;
using Content.Shared.Light.Component;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public class ExpendableLightSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var light in EntityManager.EntityQuery<ExpendableLightComponent>(true))
            {
                light.Update(frameTime);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ExpendableLightComponent, GetActivationVerbsEvent>(AddIgniteVerb);
        }

        private void AddIgniteVerb(EntityUid uid, ExpendableLightComponent component, GetActivationVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (component.CurrentState != ExpendableLightState.BrandNew)
                return;

            // Ignite the flare or make the glowstick glow.
            // Also hot damn, those are some shitty glowsticks, we need to get a refund.
            Verb verb = new();
            verb.Text = Loc.GetString("expendable-light-start-verb");
            verb.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            verb.Act = () => component.TryActivate();
            args.Verbs.Add(verb);
        }
    }
}
