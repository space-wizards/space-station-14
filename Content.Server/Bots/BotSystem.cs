using Content.Shared.MobState;

namespace Content.Server.Bots
{
    public sealed class BotSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BotComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnMobStateChanged(EntityUid uid, BotComponent component, MobStateChangedEvent args)
        {
            if (args.Component.IsCritical() || args.Component.IsDead())
                QueueDel(uid);
        }
    }
}
