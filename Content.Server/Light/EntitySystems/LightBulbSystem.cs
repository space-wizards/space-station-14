using Content.Server.Light.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.EntitySystems
{
    public sealed class LightBulbSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LightBulbComponent, LandEvent>(HandleLand);
        }

        private void HandleLand(EntityUid uid, LightBulbComponent component, LandEvent args)
        {
            component.PlayBreakSound();
            component.State = LightBulbState.Broken;
        }
    }
}
