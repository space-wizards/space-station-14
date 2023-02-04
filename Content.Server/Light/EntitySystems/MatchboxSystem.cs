using Content.Server.Light.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Smoking;

namespace Content.Server.Light.EntitySystems
{
    public sealed class MatchboxSystem : EntitySystem
    {
        [Dependency] private readonly MatchstickSystem _stickSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MatchboxComponent, InteractUsingEvent>(OnInteractUsing, before: new[] { typeof(StorageSystem) });
        }

        private void OnInteractUsing(EntityUid uid, MatchboxComponent component, InteractUsingEvent args)
        {
            if (!args.Handled
                && EntityManager.TryGetComponent<MatchstickComponent?>(args.Used, out var matchstick)
                && matchstick.CurrentState == SmokableState.Unlit)
            {
                _stickSystem.Ignite(uid, matchstick, args.User);
                args.Handled = true;
            }
        }
    }
}
