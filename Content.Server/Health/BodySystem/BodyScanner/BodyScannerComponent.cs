using System.Collections.Generic;
using Content.Server.BodySystem;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Health.BodySystem.BodyParts;
using Content.Shared.BodySystem;
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
        private BodyScannerInterfaceState PrepareBodyScannerInterfaceState(BodyTemplate template, IReadOnlyDictionary<string, BodyPart> bodyParts)
        {
            var partsData = new Dictionary<string, BodyScannerBodyPartData>();

            foreach (var(slotName, part) in bodyParts) {
                var mechanismData = new List<BodyScannerMechanismData>();

                foreach (var mech in part.Mechanisms)
                {
                    mechanismData.Add(new BodyScannerMechanismData(mech.Name, mech.Description, mech.RSIPath, mech.RSIState, mech.MaxDurability, mech.CurrentDurability));
                }
                partsData.Add(slotName, new BodyScannerBodyPartData(part.Name, part.RSIPath, part.RSIState, part.MaxDurability, part.CurrentDurability, mechanismData));
            }

            var templateData = new BodyScannerTemplateData(template.Name, template.Slots);

            return new BodyScannerInterfaceState(partsData, templateData);
        }

    }
}
