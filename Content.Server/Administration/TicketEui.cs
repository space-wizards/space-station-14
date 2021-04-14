using Content.Server.Eui;
using Content.Server.Players;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Shared.GameObjects;

namespace Content.Server.Administration
{
    public class TicketEui : BaseEui
    {
        //private readonly IEntity _newMob;

        /*public TicketEui(IEntity newMob)
        {
            _newMob = newMob;
        }*/

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptCloningChoiceMessage choice
                || choice.Button == AcceptCloningUiButton.Deny)
            {
                Close();
                return;
            }

            var mind = Player.ContentData()?.Mind;
            //mind?.TransferTo(_newMob);
            mind?.UnVisit();
            Close();
        }
    }
}
