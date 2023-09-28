using Content.Server.EUI;
using Content.Shared.Cloning;
using Content.Shared.Eui;
using Content.Server.Mind;

namespace Content.Server.SS220.DeathReminder.DeathReminderEui
{
    public sealed class DeathReminderEui : BaseEui
    {


        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptCloningChoiceMessage choice ||
                choice.Button == AcceptCloningUiButton.Deny)
            {
                Close();
                return;
            }

            Close();
        }
    }
}
