using System;
using Content.Server.Eui;
using Content.Shared.Chemistry;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Chemistry
{
    public class TransferAmountEui : BaseEui
    {
        private SolutionTransferComponent Comp;

        public TransferAmountEui(SolutionTransferComponent comp)
        {
            Comp = comp;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);
            switch (msg)
            {
                case TransferAmountEuiMessage amt:
                    var amount = Math.Clamp(amt.Value.Int(), Comp.MinimumTransferAmount.Int(),
                        Comp.MaximumTransferAmount.Int());
                    Player.AttachedEntity?.PopupMessage(Loc.GetString("comp-solution-transfer-set-amount", ("amount", amount)));
                    Comp.SetTransferAmount(ReagentUnit.New(amount));
                    break;
            }
            Close();
        }
    }
}
