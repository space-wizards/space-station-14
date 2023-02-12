using Content.Server.EUI;
using Content.Shared.Cloning;
using Content.Shared.Eui;

namespace Content.Server.Cloning
{
    public sealed class AcceptCloningEui : BaseEui
    {
        private readonly CloningSystem _cloningSystem;
        private readonly Mind.Mind _mind;

        public AcceptCloningEui(Mind.Mind mind, CloningSystem cloningSys)
        {
            _mind = mind;
            _cloningSystem = cloningSys;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptCloningChoiceMessage choice ||
                choice.Button == AcceptCloningUiButton.Deny)
            {
                Close();
                return;
            }

            _cloningSystem.TransferMindToClone(_mind);
            Close();
        }
    }
}
