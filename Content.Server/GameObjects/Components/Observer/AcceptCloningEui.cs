using Content.Server.Eui;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Mobs;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Observer
{
    public class AcceptCloningEui : BaseEui
    {
        private readonly Mind _mind;

        public AcceptCloningEui(Mind mind)
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
