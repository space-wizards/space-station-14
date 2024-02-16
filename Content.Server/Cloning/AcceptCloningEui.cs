using Content.Server.EUI;
using Content.Shared.Cloning;
using Content.Shared.Eui;
using Content.Shared.Mind;

namespace Content.Server.Cloning
{
    public sealed class AcceptCloningEui : BaseEui
    {
        private readonly EntityUid _mindId;
        private readonly MindComponent _mind;
        private readonly CloningSystem _cloningSystem;

        public AcceptCloningEui(EntityUid mindId, MindComponent mind, CloningSystem cloningSys)
        {
            _mindId = mindId;
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

            _cloningSystem.TransferMindToClone(_mindId, _mind);
            Close();
        }
    }
}
