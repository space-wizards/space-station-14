using Content.Server.Light.Components;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Light.EntitySystems
{
    public sealed class MatchboxSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MatchboxComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnInteractUsing(EntityUid uid, MatchboxComponent component, InteractUsingEvent args)
        {
            if (!args.Handled
                && EntityManager.TryGetComponent<MatchstickComponent?>(args.Used, out var matchstick)
                && matchstick.CurrentState == SmokableState.Unlit)
            {
                Get<MatchstickSystem>().Ignite(matchstick, args.User);
                args.Handled = true;
            }
        }
    }
}
