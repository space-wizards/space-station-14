#nullable enable
using System.Collections.Generic;
using Content.Server.Body;
using Content.Server.Utility;
using Content.Shared.Body.Scanner;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class BodyScannerComponent : Component, IActivate
    {
        public sealed override string Name => "BodyScanner";

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(BodyScannerUiKey.Key);

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor) ||
                actor.playerSession.AttachedEntity == null)
            {
                return;
            }

            if (actor.playerSession.AttachedEntity.TryGetComponent(out BodyManagerComponent? attempt))
            {
                var state = InterfaceState(attempt.Template, attempt.Parts);
                UserInterface?.SetState(state);
            }

            UserInterface?.Open(actor.playerSession);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface == null)
            {
                Logger.Warning($"Entity {Owner} at {Owner.Transform.MapPosition} doesn't have a {nameof(ServerUserInterfaceComponent)}");
            }
            else
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg) { }

        /// <summary>
        ///     Copy BodyTemplate and BodyPart data into a common data class that the client can read.
        /// </summary>
        private BodyScannerInterfaceState InterfaceState(BodyTemplate template, IReadOnlyDictionary<string, BodyPart> bodyParts)
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
