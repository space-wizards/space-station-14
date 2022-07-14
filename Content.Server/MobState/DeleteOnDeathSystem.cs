using Content.Shared.MobState;
using Content.Server.MobState.Components;

namespace Content.Server.MobState
{
    public sealed class BotSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DeleteOnDeathComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnMobStateChanged(EntityUid uid, DeleteOnDeathComponent component, MobStateChangedEvent args)
        {
            if (_mobStateSystem.IsDead(uid))
                QueueDel(uid);
        }
    }
}
