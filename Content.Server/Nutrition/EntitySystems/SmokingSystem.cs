using Content.Server.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Shared.GameObjects;

namespace Content.Server.Nutrition.EntitySystems
{
    public class SmokingSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<SmokingComponent, IsHotEvent>(OnIsHotEvent);
        }

        private void OnIsHotEvent(EntityUid uid, SmokingComponent component, IsHotEvent args)
        {
            args.IsHot = component.CurrentState == SharedBurningStates.Lit;
        }
    }
}
