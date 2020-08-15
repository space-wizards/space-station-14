using System.Collections.Generic;
using Content.Shared.Health.BodySystem.BodyScanner;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Health.BodySystem.BodyScanner
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

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            if (actor.playerSession.AttachedEntity.TryGetComponent(out BodyManagerComponent attempt))
            {
                _userInterface.SetState(PrepareBodyScannerInterfaceState(attempt.Template, attempt.PartDictionary));
            }
            _userInterface.Open(actor.playerSession);
        }


        /// <summary>
        ///    Copy BodyTemplate and BodyPart data into a common data class that the client can read.
        /// </summary>
        private BodyScannerInterfaceState PrepareBodyScannerInterfaceState(BodyTemplate.BodyTemplate template, Dictionary<string, BodyPart.BodyPart> bodyParts)
        {
            Dictionary<string, BodyScannerBodyPartData> partsData = new Dictionary<string, BodyScannerBodyPartData>();
            foreach (var(slotname, bpart) in bodyParts) {
                List<BodyScannerMechanismData> mechanismData = new List<BodyScannerMechanismData>();
                foreach (var mech in bpart.Mechanisms)
                {
                    mechanismData.Add(new BodyScannerMechanismData(mech.Name, mech.Description, mech.RSIPath, mech.RSIState, mech.MaxDurability, mech.CurrentDurability));
                }
                partsData.Add(slotname, new BodyScannerBodyPartData(bpart.Name, bpart.RSIPath, bpart.RSIState, bpart.MaxDurability, bpart.CurrentDurability, mechanismData));
            }
            BodyScannerTemplateData templateData = new BodyScannerTemplateData(template.Name, template.Slots);
            return new BodyScannerInterfaceState(partsData, templateData);
        }

    }
}
