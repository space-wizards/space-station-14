using Content.Server.EUI;
using Content.Shared.Cloning;
using Content.Shared.Eui;
using Robust.Shared.GameObjects;

namespace Content.Server.Cloning
{
    public sealed class AcceptCloningEui : BaseEui
    {
        private readonly Mind.Mind _mind;

        public AcceptCloningEui(Mind.Mind mind)
        {
            _mind = mind;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptCloningChoiceMessage choice ||
                choice.Button == AcceptCloningUiButton.Deny ||
                !EntitySystem.TryGet<CloningSystem>(out var cloningSystem))
            {
                Close();
                return;
            }

            cloningSystem.TransferMindToClone(_mind);
            Close();
        }
    }
}
