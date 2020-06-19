using System;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Utility;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    internal class ConstructionSystem : Shared.GameObjects.EntitySystems.ConstructionSystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IServerEntityManager _serverEntityManager;
#pragma warning restore 649

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<TryStartStructureConstructionMessage>(HandleStartStructureConstruction);
            SubscribeNetworkEvent<TryStartItemConstructionMessage>(HandleStartItemConstruction);
        }

        private void HandleStartStructureConstruction(TryStartStructureConstructionMessage msg, EntitySessionEventArgs args)
        {
            var placingEnt = args.SenderSession.AttachedEntity;
            var result = TryStartStructureConstruction(placingEnt, msg.Location, msg.PrototypeName, msg.Angle);
            if (!result) return;
            var responseMsg = new AckStructureConstructionMessage(msg.Ack);
            var channel = ((IPlayerSession) args.SenderSession).ConnectedClient;
            RaiseNetworkEvent(responseMsg, channel);
        }

        private void HandleStartItemConstruction(TryStartItemConstructionMessage msg, EntitySessionEventArgs args)
        {
            var placingEnt = args.SenderSession.AttachedEntity;
            TryStartItemConstruction(placingEnt, msg.PrototypeName);
        }

        private bool TryStartStructureConstruction(IEntity placingEnt, GridCoordinates loc, string prototypeName, Angle angle)
        {
            var prototype = _prototypeManager.Index<ConstructionPrototype>(prototypeName);

            if (!InteractionChecks.InRangeUnobstructed(placingEnt, loc.ToMap(_mapManager),
                ignoredEnt: placingEnt, insideBlockerValid: prototype.CanBuildInImpassable))
            {
                return false;
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
            var hands = placingEnt.GetComponent<HandsComponent>();
            var activeHand = hands.GetActiveHand?.Owner;
            if (activeHand == null)
            {
                return false;
            }

            if (!activeHand.TryGetComponent(out StackComponent stack) || !ConstructionComponent.MaterialStackValidFor(matStep, stack))
            {
                return false;
            }

            if (!stack.Use(matStep.Amount))
            {
                return false;
            }

            // OK WE'RE GOOD CONSTRUCTION STARTED.
            Get<AudioSystem>().PlayAtCoords("/Audio/items/deconstruct.ogg", loc);
            if (prototype.Stages.Count == 2)
            {
                // Exactly 2 stages, so don't make an intermediate frame.
                var ent = _serverEntityManager.SpawnEntity(prototype.Result, loc);
                ent.Transform.LocalRotation = angle;
            }
            else
            {
                var frame = _serverEntityManager.SpawnEntity("structureconstructionframe", loc);
                var construction = frame.GetComponent<ConstructionComponent>();
                construction.Init(prototype);
                frame.Transform.LocalRotation = angle;
            }

            return true;
        }

        private void TryStartItemConstruction(IEntity placingEnt, string prototypeName)
        {
            var prototype = _prototypeManager.Index<ConstructionPrototype>(prototypeName);

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
            var hands = placingEnt.GetComponent<HandsComponent>();
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
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/items/deconstruct.ogg", placingEnt);
            if (prototype.Stages.Count == 2)
            {
                // Exactly 2 stages, so don't make an intermediate frame.
                var ent = _serverEntityManager.SpawnEntity(prototype.Result, placingEnt.Transform.GridPosition);
                hands.PutInHandOrDrop(ent.GetComponent<ItemComponent>());
            }
            else
            {
                //TODO: Make these viable as an item and try putting them in the players hands
                var frame = _serverEntityManager.SpawnEntity("structureconstructionframe", placingEnt.Transform.GridPosition);
                var construction = frame.GetComponent<ConstructionComponent>();
                construction.Init(prototype);
            }

        }
    }
}
