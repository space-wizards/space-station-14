using Content.Server.Cooldown;
using Content.Server.Popups;
using Content.Server.Sports.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Sports
{

    public sealed class BallTagSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BallTagComponent, AfterInteractEvent>(AfterInteractEvent);
        }

        private void AfterInteractEvent(EntityUid uid, BallTagComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            var currentTime = _gameTiming.CurTime;
            if (currentTime < component.CooldownEnd)
                return;

            if (args.Target == null || args.Target == args.User)
                return;

            component.LastUseTime = currentTime;
            component.CooldownEnd = component.LastUseTime + TimeSpan.FromSeconds(component.CooldownTime);

            RaiseLocalEvent(uid, new RefreshItemCooldownEvent(component.LastUseTime, component.CooldownEnd), false);

            _popupSystem.PopupEntity(Loc.GetString("sports-baseball-tag-popup", ("user", args.User), ("target", args.Target), ("used", args.Used)), args.User, Filter.Pvs(args.User), PopupType.Medium);
        }

    }
}
