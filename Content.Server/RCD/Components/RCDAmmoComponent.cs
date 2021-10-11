using System;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.RCD.Components
{
    [RegisterComponent]
    public class RCDAmmoComponent : Component, IAfterInteract, IExamine
    {
        public override string Name => "RCDAmmo";

        //How much ammo we refill
        [ViewVariables(VVAccess.ReadWrite)] [DataField("refillAmmo")] private int refillAmmo = 5;

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("rcd-ammo-component-on-examine-text",("ammo", refillAmmo)));
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null ||
                !eventArgs.Target.TryGetComponent(out RCDComponent? rcdComponent) ||
                !eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                return false;
            }

            if (rcdComponent.MaxAmmo - rcdComponent._ammo < refillAmmo)
            {
                rcdComponent.Owner.PopupMessage(eventArgs.User, Loc.GetString("rcd-ammo-component-after-interact-full-text"));
                return true;
            }

            rcdComponent._ammo = Math.Min(rcdComponent.MaxAmmo, rcdComponent._ammo + refillAmmo);
            rcdComponent.Owner.PopupMessage(eventArgs.User, Loc.GetString("rcd-ammo-component-after-interact-refilled-text"));

            //Deleting a held item causes a lot of errors
            hands.Drop(Owner, false);
            Owner.Delete();
            return true;
        }
    }
}
