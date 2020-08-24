using System;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.RCD
{
    [RegisterComponent]
    public class RCDAmmoComponent : Component, IAfterInteract, IExamine
    {
        [Dependency] private IServerNotifyManager _serverNotifyManager = default!;

        public override string Name => "RCDAmmo";

        //How much ammo we refill
        [ViewVariables(VVAccess.ReadWrite)] private int refillAmmo = 5;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref refillAmmo, "refillAmmo", 5);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("It holds {0} charges.", refillAmmo));
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs   eventArgs)
        {
            if (eventArgs.Target == null || !eventArgs.Target.TryGetComponent(out RCDComponent rcdComponent) || !eventArgs.User.TryGetComponent(out IHandsComponent hands))
            {
                return;
            }

            if (rcdComponent.maxAmmo - rcdComponent._ammo < refillAmmo)
            {
                _serverNotifyManager.PopupMessage(rcdComponent.Owner, eventArgs.User, "The RCD is full!");
                return;
            }

            rcdComponent._ammo = Math.Min(rcdComponent.maxAmmo, rcdComponent._ammo + refillAmmo);
            _serverNotifyManager.PopupMessage(rcdComponent.Owner, eventArgs.User, "You refill the RCD.");

            //Deleting a held item causes a lot of errors
            hands.Drop(Owner, false);
            Owner.Delete();

        }
    }
}
