using System;
using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public class ItemCooldownSystem : EntitySystem
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

    public class RefreshItemCooldownEvent : EntityEventArgs
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
