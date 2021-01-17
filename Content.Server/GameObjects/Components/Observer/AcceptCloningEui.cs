#nullable enable
using Content.Server.Eui;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Players;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Observer
{
    public class AcceptCloningEui : BaseEui
    {
        private readonly IEntity _newMob;

        public AcceptCloningEui(IEntity newMob)
        {
            _newMob = newMob;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptCloningChoiceMessage choice
            || choice.Button == AcceptCloningUiButton.Deny
            || _newMob.Deleted)
            {
                Close();
                return;
            }

            Player.ContentData()?.Mind?.TransferTo(_newMob);
            Close();
        }
    }
}
