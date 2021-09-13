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
            foreach (var light in ComponentManager.EntityQuery<ExpendableLightComponent>(true))
            {
                light.Update(frameTime);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ExpendableLightComponent, GetInteractionVerbsEvent>(AddInteractionVerb);
        }

        private void AddInteractionVerb(EntityUid uid, ExpendableLightComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (component.CurrentState != ExpendableLightState.BrandNew)
                return;

            Verb verb = new("ExpendableLight:Ignite");
            verb.Text = Loc.GetString("expendable-light-start-verb");
            verb.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            verb.Act = () => component.TryActivate();
            args.Verbs.Add(verb);
        }
    }
}
