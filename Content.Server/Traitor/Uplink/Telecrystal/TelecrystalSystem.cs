using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System;

namespace Content.Server.Traitor.Uplink.Telecrystal
{
    public sealed class TelecrystalSystem : EntitySystem
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
            if (args.Handled || !args.CanReach)
                return;

            if (args.Target == null || !EntityManager.TryGetComponent(args.Target.Value, out UplinkComponent? uplink))
                return;

            // TODO: when uplink will have some auth logic (like PDA ringtone code)
            // check if uplink open before adding TC
            // No metagaming by using this on every PDA around just to see if it gets used up.

            var acc = uplink.UplinkAccount;
            if (acc == null)
                return;

            EntityManager.TryGetComponent(uid, out SharedStackComponent? stack);

            var tcCount = stack != null ? stack.Count : 1;
            if (!_accounts.AddToBalance(acc, tcCount))
                return;

            EntityManager.DeleteEntity(uid);

            var msg = Loc.GetString("telecrystal-component-sucs-inserted",
                ("source", args.Used), ("target", args.Target));
            args.User.PopupMessage(args.User, msg);

            args.Handled = true;
        }
    }
}
