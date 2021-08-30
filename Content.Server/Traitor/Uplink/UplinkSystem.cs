using Content.Server.PDA.Managers;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.Traitor.Uplink.Events;
using Content.Shared.PDA;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System;

namespace Content.Server.Traitor.Uplink.Systems
{
    public class UplinkSystem : EntitySystem
    {
        [Dependency] private readonly IUplinkManager _uplinkManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UplinkComponent, TryInitUplinkEvent>(InitUplinkAccount);
        }

        private void InitUplinkAccount(EntityUid uid, UplinkComponent uplink, TryInitUplinkEvent args)
        {
            var acc = args.Account;
            uplink.SyndicateUplinkAccount = acc;
            _uplinkManager.AddNewAccount(acc);


            RaiseLocalEvent(uid, new UplinkInitEvent(uplink));

            /*_syndicateUplinkAccount.BalanceChanged += account =>
            {
                //UpdatePDAUserInterface();
            };*/


        }

        /*public void InitUplinkAccount(UplinkAccount acc)
        {
            syndicateUplinkAccount = acc;
            _uplinkManager.AddNewAccount(_syndicateUplinkAccount);

            _syndicateUplinkAccount.BalanceChanged += account =>
            {
                //UpdatePDAUserInterface();
            };

            //UpdatePDAUserInterface();
    }*/
    }
}
