using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Materials;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components.Construction;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Construction
{
    public class ConstructorComponent : SharedConstructorComponent
    {
        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case TryStartStructureConstructionMessage tryStart:
                    TryStartStructureConstruction(tryStart.Location, tryStart.PrototypeName, tryStart.Ack);
                    break;
            }
        }

        void TryStartStructureConstruction(GridLocalCoordinates loc, string prototypeName, int ack)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            var prototype = protoMan.Index<ConstructionPrototype>(prototypeName);

            var transform = Owner.GetComponent<ITransformComponent>();
            if (!loc.InRange(transform.LocalPosition, InteractionSystem.INTERACTION_RANGE))
            {
                return;
            }

            if (prototype.Stages.Count < 2)
            {
                throw new InvalidOperationException($"Prototype '{prototypeName}' does not have enough stages.");
            }

            var stage0 = prototype.Stages[0];
            if (!(stage0.Forward is ConstructionStepMaterial matStep))
            {
                throw new NotImplementedException();
            }

            // Try to find the stack with the material in the user's hand.
            var hands = Owner.GetComponent<HandsComponent>();
            var activeHand = hands.GetActiveHand?.Owner;
            if (activeHand == null)
            {
                return;
            }

            if (!activeHand.TryGetComponent(out StackComponent stack) || !ConstructionComponent.MaterialStackValidFor(matStep, stack))
            {
                return;
            }

            if (!stack.Use(matStep.Amount))
            {
                return;
            }

            // OK WE'RE GOOD CONSTRUCTION STARTED.
            var entMgr = IoCManager.Resolve<IServerEntityManager>();
            if (prototype.Stages.Count == 2)
            {
                // Exactly 2 stages, so don't make an intermediate frame.
                entMgr.ForceSpawnEntityAt(prototype.Result, loc);
            }
            else
            {
                var frame = entMgr.ForceSpawnEntityAt("structureconstructionframe", loc);
                var construction = frame.GetComponent<ConstructionComponent>();
                construction.Init(prototype);
            }

            var msg = new AckStructureConstructionMessage(ack);
            SendNetworkMessage(msg);
        }
    }
}
