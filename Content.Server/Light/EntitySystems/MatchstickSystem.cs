using Content.Server.Items;
using Content.Server.Light.Components;
using Content.Server.Ignitable;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.EntitySystems
{
    public class MatchstickSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MatchstickComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnInteractUsing(EntityUid uid, MatchstickComponent component, InteractUsingEvent args)
        {
            //Grab Ignitable Component, Set State
            //Check our current Ignitable state and Set Candle state based on that
            if (TryComp<IgnitableComponent>(uid, out var ignitableComponent))
            {
                SetState(component, ignitableComponent.CurrentState);
            }
        }

        public void SetState(MatchstickComponent component, SmokableState value)
        {
            component.CurrentState = value;

            if (component.PointLightComponent != null)
            {
                component.PointLightComponent.Enabled = component.CurrentState == SmokableState.Lit;
            }

            if (EntityManager.TryGetComponent(component.Owner, out ItemComponent? item))
            {
                switch (component.CurrentState)
                {
                    case SmokableState.Lit:
                        item.EquippedPrefix = "lit";
                        break;
                    default:
                        item.EquippedPrefix = "unlit";
                        break;
                }
            }

            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(SmokingVisuals.Smoking, component.CurrentState);
            }
        }
    }
}
