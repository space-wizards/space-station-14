using System.Collections.Generic;
using Content.Server.Body;
using Content.Shared.Body.Scanner;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class BodyScannerComponent : Component, IActivate
    {
        private BoundUserInterface _userInterface;
        public sealed override string Name => "BodyScanner";

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor) ||
                actor.playerSession.AttachedEntity == null)
            {
                return;
            }

            if (actor.playerSession.AttachedEntity.TryGetComponent(out BodyManagerComponent attempt))
            {
                _userInterface.SetState(InterfaceState(attempt.Template, attempt.Parts));
            }

            _userInterface.Open(actor.playerSession);
        }

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(BodyScannerUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg) { }

        /// <summary>
        ///     Copy BodyTemplate and BodyPart data into a common data class that the client can read.
        /// </summary>
        private BodyScannerInterfaceState InterfaceState(BodyTemplate template, Dictionary<string, BodyPart> bodyParts)
        {
            var partsData = new Dictionary<string, BodyScannerBodyPartData>();

            foreach (var (slotName, part) in bodyParts)
            {
                var mechanismData = new List<BodyScannerMechanismData>();

                foreach (var mechanism in part.Mechanisms)
                {
                    mechanismData.Add(new BodyScannerMechanismData(mechanism.Name, mechanism.Description,
                        mechanism.RSIPath,
                        mechanism.RSIState, mechanism.MaxDurability, mechanism.CurrentDurability));
                }

                partsData.Add(slotName,
                    new BodyScannerBodyPartData(part.Name, part.RSIPath, part.RSIState, part.MaxDurability,
                        part.CurrentDurability, mechanismData));
            }

            var templateData = new BodyScannerTemplateData(template.Name, template.Slots);

            return new BodyScannerInterfaceState(partsData, templateData);
        }
    }
}
