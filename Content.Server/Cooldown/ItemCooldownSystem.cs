using Content.Shared.Cooldown;

namespace Content.Server.Cooldown
{
    public sealed class ItemCooldownSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemCooldownComponent, RefreshItemCooldownEvent>(OnItemCooldownRefreshed);
        }

        public void OnItemCooldownRefreshed(EntityUid uid, ItemCooldownComponent comp, RefreshItemCooldownEvent args)
        {
            comp.CooldownStart = args.LastAttackTime;
            comp.CooldownEnd = args.CooldownEnd;
        }
    }

    public sealed class RefreshItemCooldownEvent : EntityEventArgs
    {
        public TimeSpan LastAttackTime { get; }
        public TimeSpan CooldownEnd { get;  }

        public RefreshItemCooldownEvent(TimeSpan lastAttackTime, TimeSpan cooldownEnd)
        {
            LastAttackTime = lastAttackTime;
            CooldownEnd = cooldownEnd;
        }
    }
}
