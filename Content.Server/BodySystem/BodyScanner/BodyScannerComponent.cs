using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.BodySystem;


namespace Content.Server.BodySystem
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class BodyScannerComponent : Component, IActivate
    {
        public sealed override string Name => "BodyScanner";

        private BoundUserInterface _userInterface;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(BodyScannerUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {

        }

        /// <summary>
        ///    Reads data from YAML
        /// </summary>
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }
            actor.playerSession.AttachedEntity.TryGetComponent(out BodyManagerComponent attempt);
            if(attempt != null)
                _userInterface.SetState(new BodyScannerInterfaceState(attempt.Template, attempt.Parts));
            _userInterface.Open(actor.playerSession);
            attempt.DisconnectBodyPart("right arm", true);
        }

    }
}
