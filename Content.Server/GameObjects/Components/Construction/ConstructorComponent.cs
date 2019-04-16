using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Materials;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components.Construction;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

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
                    TryStartStructureConstruction(tryStart.Location, tryStart.PrototypeName, tryStart.Angle, tryStart.Ack);
                    break;
            }
        }

        void TryStartStructureConstruction(GridCoordinates loc, string prototypeName, Angle angle, int ack)
        {
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            var prototype = protoMan.Index<ConstructionPrototype>(prototypeName);

            var transform = Owner.GetComponent<ITransformComponent>();
            if (!loc.InRange(transform.GridPosition, InteractionSystem.INTERACTION_RANGE))
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
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>().Play("/Audio/items/deconstruct.ogg", loc);
            if (prototype.Stages.Count == 2)
            {
                // Exactly 2 stages, so don't make an intermediate frame.
                var ent = entMgr.ForceSpawnEntityAt(prototype.Result, loc);
                ent.GetComponent<ITransformComponent>().LocalRotation = angle;
            }
            else
            {
                var frame = entMgr.ForceSpawnEntityAt("structureconstructionframe", loc);
                var construction = frame.GetComponent<ConstructionComponent>();
                construction.Init(prototype);
                frame.GetComponent<ITransformComponent>().LocalRotation = angle;
            }

            var msg = new AckStructureConstructionMessage(ack);
            SendNetworkMessage(msg);
        }
    }
}
