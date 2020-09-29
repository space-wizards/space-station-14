#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ConstructionComponent : Component, IExamine, IInteractUsing
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        public override string Name => "Construction";

        private string _graphIdentifier = string.Empty;
        private string _startingNodeIdentifier = string.Empty;

        private HashSet<string> _containers = new HashSet<string>();
        private List<List<ConstructionGraphStep>>? _edgeNestedStepProgress = null;

        public ConstructionGraph Graph { get; private set; } = null!;
        public ConstructionGraphNode Node { get; private set; } = null!;
        public ConstructionGraphEdge? Edge { get; private set; } = null;
        public int EdgeStep { get; private set; } = 0;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _graphIdentifier, "graph", string.Empty);
            serializer.DataField(ref _startingNodeIdentifier, "node", string.Empty);
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (Edge == null)
                return await HandleNode(eventArgs);

            return await HandleEdge(eventArgs);
        }

        private async Task<bool> HandleNode(InteractUsingEventArgs eventArgs)
        {
            EdgeStep = 0;

            foreach (var edge in Node.Edges)
            {
                var firstStep = edge.Steps[0];
                switch (firstStep)
                {
                    case MaterialConstructionGraphStep _:
                    case ToolConstructionGraphStep _:
                    case PrototypeConstructionGraphStep _:
                    case ComponentConstructionGraphStep _:
                        if (await HandleStep(eventArgs, edge, firstStep))
                        {
                            Edge = edge;
                            return true;
                        }
                        break;

                    case NestedConstructionGraphStep nestedStep:
                        throw new IndexOutOfRangeException($"Nested construction step not supported as the first step in an edge! Graph: {Graph.ID} Node: {Node.Name} Edge: {edge.Target}");
                }
            }

            return false;
        }

        private async Task<bool> HandleStep(InteractUsingEventArgs eventArgs, ConstructionGraphEdge? edge = null, ConstructionGraphStep? step = null)
        {
            edge ??= Edge;
            step ??= edge?.Steps[EdgeStep];

            if (edge == null || step == null)
                return false;

            foreach (var condition in edge.Conditions)
            {
                if (!await condition.Condition(Owner)) return false;
            }

            var handled = false;

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(eventArgs.User, step.DoAfter, default, eventArgs.Target)
            {
                BreakOnDamage = false, // TODO: Change this to true once breathing is fixed.
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            switch (step)
            {
                case ToolConstructionGraphStep toolStep:
                    if (eventArgs.Using.TryGetComponent(out ToolComponent? tool) &&
                        await tool.UseTool(eventArgs.User, Owner, step.DoAfter, toolStep.Tool))
                    {
                        handled = true;
                    }
                    break;

                // To prevent too much code duplication.
                case EntityInsertConstructionGraphStep insertStep:
                    var valid = false;
                    var entityUsing = eventArgs.Using;

                    switch (insertStep)
                    {
                        case PrototypeConstructionGraphStep prototypeStep:
                            if (eventArgs.Using.Prototype?.ID == prototypeStep.Prototype
                                && (await doAfterSystem.DoAfter(doAfterArgs)) == DoAfterStatus.Finished)
                            {
                                valid = true;
                            }

                            break;

                        case ComponentConstructionGraphStep componentStep:
                            if (eventArgs.Using.HasComponent(_componentFactory.GetRegistration(componentStep.Component).Type)
                                && (await doAfterSystem.DoAfter(doAfterArgs)) == DoAfterStatus.Finished)
                            {
                                valid = true;
                            }

                            break;

                        case MaterialConstructionGraphStep materialStep:
                            if (eventArgs.Using.TryGetComponent(out StackComponent? stack) && stack.StackType.Equals(materialStep.Material)
                                && (await doAfterSystem.DoAfter(doAfterArgs)) == DoAfterStatus.Finished)
                            {
                                valid = stack.Split(materialStep.Amount, eventArgs.User.Transform.Coordinates, out entityUsing);
                            }

                            break;
                    }

                    if (!valid || entityUsing == null) break;

                    if(string.IsNullOrEmpty(insertStep.Store))
                        entityUsing.Delete();
                    else
                    {
                        _containers.Add(insertStep.Store);
                        var container = ContainerManagerComponent.Ensure<Container>(insertStep.Store, Owner);
                        container.Insert(entityUsing);
                    }

                    handled = true;

                    break;

                case NestedConstructionGraphStep nestedStep:
                    // TODO CONSTRUCTION
                    break;
            }

            if (!handled) return false;

            EdgeStep++;

            if (edge.Steps.Count == EdgeStep)
            {
                await HandleCompletion(edge);
            }

            return true;
        }

        private async Task<bool> HandleCompletion(ConstructionGraphEdge edge)
        {
            if (edge.Steps.Count != EdgeStep)
            {
                return false;
            }

            foreach (var completed in edge.Completed)
            {
                await completed.Completed(Owner);
                if (Owner.Deleted) return true;
            }

            Node = Graph.Nodes[edge.Target];

            await HandleEntityChange(Node);

            return true;
        }

        private async Task<bool> HandleEdge(InteractUsingEventArgs eventArgs)
        {
            return await Task.FromResult(false);
        }

        private async Task<bool> HandleEntityChange(ConstructionGraphNode node)
        {
            if (node.Entity == Owner.Prototype?.ID) return false;

            var entity = _entityManager.SpawnEntity(node.Entity, Owner.Transform.Coordinates);

            entity.Transform.LocalRotation = Owner.Transform.LocalRotation;

            if (entity.TryGetComponent(out ConstructionComponent? construction))
            {
                if(construction.Graph != Graph)
                    throw new Exception($"New entity {node.Entity}'s graph {construction.Graph.ID} isn't the same as our graph {Graph.ID} on node {node.Name}!");

                construction.Node = node;
            }

            if (Owner.TryGetComponent(out ContainerManagerComponent? containerComp))
            {
                foreach (var container in _containers)
                {
                    var otherContainer = ContainerManagerComponent.Ensure<Container>(container, entity);
                    var ourContainer = containerComp.GetContainer(container);

                    foreach (var ent in ourContainer.ContainedEntities.ToArray())
                    {
                        ourContainer.ForceRemove(ent);
                        otherContainer.Insert(ent);
                    }
                }
            }

            Owner.Delete();

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (string.IsNullOrEmpty(_graphIdentifier))
            {
                Logger.Error($"Prototype {Owner.Prototype?.ID}'s construction component didn't have a graph identifier!");
                return;
            }

            if (_prototypeManager.TryIndex(_graphIdentifier, out ConstructionGraph graph))
            {
                Graph = graph;

                if (Graph.Nodes.TryGetValue(_startingNodeIdentifier, out var node))
                {
                    Node = node;
                }
                else
                {
                    Logger.Error($"Couldn't find node {_startingNodeIdentifier} in graph {_graphIdentifier} in construction component!");
                }
            }
            else
            {
                Logger.Error($"Couldn't find prototype {_graphIdentifier} in construction component!");
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            // EntitySystem.Get<SharedConstructionSystem>().DoExamine(message, Prototype, Stage, inDetailsRange);
        }
    }
}
