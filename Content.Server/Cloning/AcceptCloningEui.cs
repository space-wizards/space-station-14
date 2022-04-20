using Content.Server.EUI;
using Content.Shared.Cloning;
using Content.Shared.Eui;
using Content.Server.Cloning.Systems;

namespace Content.Server.Cloning
{
    public sealed class AcceptCloningEui : BaseEui
    {
        [Dependency] private readonly CloningSystem _cloningSystem = default!;
        private readonly Mind.Mind _mind;

        public AcceptCloningEui(Mind.Mind mind)
        {
            _mind = mind;
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
