#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    internal class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<TryStartStructureConstructionMessage>(HandleStartStructureConstruction);
            SubscribeNetworkEvent<TryStartItemConstructionMessage>(HandleStartItemConstruction);
        }

        private void HandleStartItemConstruction(TryStartItemConstructionMessage ev, EntitySessionEventArgs args)
        {
            // TODO CONSTRUCTION
        }

        private async void HandleStartStructureConstruction(TryStartStructureConstructionMessage ev, EntitySessionEventArgs args)
        {
            var constructionPrototype = _prototypeManager.Index<ConstructionPrototype>(ev.PrototypeName);
            var constructionGraph = _prototypeManager.Index<ConstructionGraphPrototype>(constructionPrototype.Graph);
            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];

            var user = args.SenderSession.AttachedEntity;

            if (user == null || !ActionBlockerSystem.CanInteract(user)) return;

            if (!user.TryGetComponent(out HandsComponent? hands)) return;

            if (hands.GetActiveHand == null) return;

            IEntity? holding = hands.GetActiveHand.Owner;

            var doAfterSystem = Get<DoAfterSystem>();

            foreach (var edge in startNode.Edges)
            {
                // We don't allow edges with conditions as starting edges.
                if (edge.Conditions.Count != 0) continue;

                // We definitely don't allow edges without exactly one step.
                if (edge.Steps.Count != 1) continue;

                var edgeTarget = constructionGraph.Nodes[edge.Target];

                // And we also don't allow edges whose target doesn't have an entity specified.
                if (string.IsNullOrEmpty(edgeTarget.Entity)) continue;

                    var firstStep = edge.Steps[0];

                switch (firstStep)
                {
                    // We don't allow nested construction steps as the first step.
                    case NestedConstructionGraphStep nestedStep:
                        continue;

                    // We don't allow tool construction steps as the first step either.
                    case ToolConstructionGraphStep toolStep:
                        continue;

                    case EntityInsertConstructionGraphStep insertStep:
                        var valid = false;

                        var doAfterArgs = new DoAfterEventArgs(user, insertStep.DoAfter)
                        {
                            BreakOnDamage = true,
                            BreakOnStun = true,
                            BreakOnTargetMove = false,
                            BreakOnUserMove = true,
                            NeedHand = true,
                        };

                        switch (insertStep)
                        {
                            case PrototypeConstructionGraphStep prototypeStep:
                                if (holding!.Prototype?.ID == prototypeStep.Prototype
                                    && (await doAfterSystem.DoAfter(doAfterArgs)) == DoAfterStatus.Finished)
                                {
                                    valid = true;
                                }

                                break;

                            case ComponentConstructionGraphStep componentStep:
                                if (holding!.HasComponent(
                                        _componentFactory.GetRegistration(componentStep.Component).Type)
                                    && (await doAfterSystem.DoAfter(doAfterArgs)) == DoAfterStatus.Finished)
                                {
                                    valid = true;
                                }

                                break;

                            case MaterialConstructionGraphStep materialStep:
                                if (holding!.TryGetComponent(out StackComponent? stack) &&
                                    stack.StackType.Equals(materialStep.Material)
                                    && (await doAfterSystem.DoAfter(doAfterArgs)) == DoAfterStatus.Finished)
                                {
                                    valid = stack.Split(materialStep.Amount, user.Transform.Coordinates, out holding);
                                }

                                break;
                        }

                        if (!valid || holding == null) break;

                        var entity = _entityManager.SpawnEntity(edgeTarget.Entity, ev.Location);
                        entity.Transform.LocalRotation = ev.Angle;

                        // Yes, this should throw if it's missing the component.
                        var construction = entity.GetComponent<ConstructionComponent>();

                        if(string.IsNullOrEmpty(insertStep.Store))
                            holding.Delete();
                        else
                        {
                            construction.AddContainer(insertStep.Store);
                            var container = ContainerManagerComponent.Ensure<Container>(insertStep.Store, entity);
                            container.Insert(holding);
                        }

                        // We do have completed effects!
                        foreach (var completed in edge.Completed)
                        {
                            await completed.Completed(entity);
                        }

                        RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));

                        return;
                }
            }
        }
    }
}

