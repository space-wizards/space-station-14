using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;

namespace Content.Server.Traitor.Uplink.Telecrystal
{
    public class TelecrystalSystem : EntitySystem
    {
        [Dependency]
        private readonly UplinkAccountsSystem _accounts = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TelecrystalComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, TelecrystalComponent component, AfterInteractEvent args)
        {
            if (args.Target == null || !EntityManager.TryGetComponent(args.Target.Uid, out UplinkComponent? uplink))
                return;

            var acc = uplink.UplinkAccount;
            if (acc == null)
                return;

            EntityManager.TryGetComponent(uid, out SharedStackComponent? stack);

            var tcCount = stack != null ? stack.Count : 1;
            _accounts.AddToBalance(acc, tcCount);

            EntityManager.DeleteEntity(uid);
        }
    }
}
