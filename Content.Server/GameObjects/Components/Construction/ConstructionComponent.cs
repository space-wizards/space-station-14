#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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

        public override string Name => "Construction";

        private bool _handling = false;

        private TaskCompletionSource<object>? _handlingTask = null;
        private string _graphIdentifier = string.Empty;
        private string _startingNodeIdentifier = string.Empty;

        [ViewVariables]
        private HashSet<string> _containers = new();
        [ViewVariables]
        private List<List<ConstructionGraphStep>>? _edgeNestedStepProgress = null;

        private ConstructionGraphNode? _target = null;

        [ViewVariables]
        public ConstructionGraphPrototype? GraphPrototype { get; private set; }

        [ViewVariables]
        public ConstructionGraphNode? Node { get; private set; } = null;

        [ViewVariables]
        public ConstructionGraphEdge? Edge { get; private set; } = null;

        public IReadOnlyCollection<string> Containers => _containers;

        [ViewVariables]
        public ConstructionGraphNode? Target
        {
            get => _target;
            set
            {
                ClearTarget();
                _target = value;
                UpdateTarget();
            }
        }

        [ViewVariables]
        public ConstructionGraphEdge? TargetNextEdge { get; private set; } = null;

        [ViewVariables]
        public Queue<ConstructionGraphNode>? TargetPathfinding { get; private set; } = null;

        [ViewVariables]
        public int EdgeStep { get; private set; } = 0;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _graphIdentifier, "graph", string.Empty);
            serializer.DataField(ref _startingNodeIdentifier, "node", string.Empty);
        }

        /// <summary>
        ///     Attempts to set a new pathfinding target.
        /// </summary>
        public void SetNewTarget(string node)
        {
            if (GraphPrototype != null && GraphPrototype.Nodes.TryGetValue(node, out var target))
            {
                Target = target;
            }
        }

        public void ClearTarget()
        {
            _target = null;
            TargetNextEdge = null;
            TargetPathfinding = null;
        }

        public void UpdateTarget()
        {
            // Can't pathfind without a target or no node.
            if (Target == null || Node == null || GraphPrototype == null) return;

            // If we're at our target, stop pathfinding.
            if (Target == Node)
            {
                ClearTarget();
                return;
            }

            // If we don't have the path, set it!
            if (TargetPathfinding == null)
            {
                var path = GraphPrototype.Path(Node.Name, Target.Name);

                if (path == null)
                {
                    ClearTarget();
                    return;
                }

                TargetPathfinding = new Queue<ConstructionGraphNode>(path);
            }

            // Dequeue the pathfinding queue if the next is the node we're at.
            if (TargetPathfinding.Peek() == Node)
                TargetPathfinding.Dequeue();

            // If we went the wrong way, we stop pathfinding.
            if (Edge != null && TargetNextEdge != Edge)
            {
                ClearTarget();
                return;
            }

            // Let's set the next target edge.
            if (Edge == null && TargetNextEdge == null && TargetPathfinding != null)
                TargetNextEdge = Node.GetEdge(TargetPathfinding.Peek().Name);
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_handling)
                return true;

            _handlingTask = new TaskCompletionSource<object>();
            _handling = true;
            bool result;

            if (Edge == null)
                result = await HandleNode(eventArgs);
            else
                result = await HandleEdge(eventArgs);

            _handling = false;
            _handlingTask.SetResult(null!);

            return result;
        }

        private async Task<bool> HandleNode(InteractUsingEventArgs eventArgs)
        {
            EdgeStep = 0;

            if (Node == null || GraphPrototype == null) return false;

            foreach (var edge in Node.Edges)
            {
                if(edge.Steps.Count == 0)
                    throw new InvalidDataException($"Edge to \"{edge.Target}\" in node \"{Node.Name}\" of graph \"{GraphPrototype.ID}\" doesn't have any steps!");

                var firstStep = edge.Steps[0];
                switch (firstStep)
                {
                    case MaterialConstructionGraphStep _:
                    case ToolConstructionGraphStep _:
                    case PrototypeConstructionGraphStep _:
                    case ComponentConstructionGraphStep _:
                        if (await HandleStep(eventArgs, edge, firstStep))
                        {
                            if(edge.Steps.Count > 1)
                                Edge = edge;
                            return true;
                        }
                        break;

                    case NestedConstructionGraphStep nestedStep:
                        throw new IndexOutOfRangeException($"Nested construction step not supported as the first step in an edge! Graph: {GraphPrototype.ID} Node: {Node.Name} Edge: {edge.Target}");
                }
            }

            return false;
        }

        private async Task<bool> HandleStep(InteractUsingEventArgs eventArgs, ConstructionGraphEdge? edge = null, ConstructionGraphStep? step = null, bool nested = false)
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
                BreakOnDamage = false,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            switch (step)
            {
                case ToolConstructionGraphStep toolStep:
                    // Gotta take welder fuel into consideration.
                    if (toolStep.Tool == ToolQuality.Welding)
                    {
                        if (eventArgs.Using.TryGetComponent(out WelderComponent? welder) &&
                            await welder.UseTool(eventArgs.User, Owner, step.DoAfter, toolStep.Tool, toolStep.Fuel))
                        {
                            handled = true;
                        }
                        break;
                    }

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
                            if (prototypeStep.EntityValid(eventArgs.Using)
                                && await doAfterSystem.DoAfter(doAfterArgs) == DoAfterStatus.Finished)
                            {
                                valid = true;
                            }

                            break;

                        case ComponentConstructionGraphStep componentStep:
                            if (componentStep.EntityValid(eventArgs.Using)
                                && await doAfterSystem.DoAfter(doAfterArgs) == DoAfterStatus.Finished)
                            {
                                valid = true;
                            }

                            break;

                        case MaterialConstructionGraphStep materialStep:
                            if (materialStep.EntityValid(eventArgs.Using, out var sharedStack)
                                && await doAfterSystem.DoAfter(doAfterArgs) == DoAfterStatus.Finished)
                            {
                                var stack = (StackComponent) sharedStack;
                                valid = stack.Split(materialStep.Amount, eventArgs.User.Transform.Coordinates, out entityUsing);
                            }

                            break;
                    }

                    if (!valid || entityUsing == null) break;

                    if(string.IsNullOrEmpty(insertStep.Store))
                    {
                        entityUsing.Delete();
                    }
                    else
                    {
                        _containers.Add(insertStep.Store);
                        var container = ContainerManagerComponent.Ensure<Container>(insertStep.Store, Owner);
                        container.Insert(entityUsing);
                    }

                    handled = true;

                    break;

                case NestedConstructionGraphStep nestedStep:
                    if(_edgeNestedStepProgress == null)
                        _edgeNestedStepProgress = new List<List<ConstructionGraphStep>>(nestedStep.Steps);

                    foreach (var list in _edgeNestedStepProgress.ToArray())
                    {
                        if (list.Count == 0)
                        {
                            _edgeNestedStepProgress.Remove(list);
                            continue;
                        }

                        if (!await HandleStep(eventArgs, edge, list[0], true)) continue;

                        list.RemoveAt(0);

                        // We check again...
                        if (list.Count == 0)
                            _edgeNestedStepProgress.Remove(list);
                    }

                    if (_edgeNestedStepProgress.Count == 0)
                        handled = true;

                    break;
            }

            if (handled)
            {
                foreach (var completed in step.Completed)
                {
                    await completed.PerformAction(Owner, eventArgs.User);

                    if (Owner.Deleted)
                        return false;
                }
            }

            if (nested && handled) return true;
            if (!handled) return false;

            EdgeStep++;

            if (edge.Steps.Count == EdgeStep)
            {
                await HandleCompletion(edge, eventArgs.User);
            }

            UpdateTarget();

            return true;
        }

        private async Task<bool> HandleCompletion(ConstructionGraphEdge edge, IEntity user)
        {
            if (edge.Steps.Count != EdgeStep || GraphPrototype == null)
            {
                return false;
            }

            Edge = edge;

            UpdateTarget();

            TargetNextEdge = null;
            Edge = null;
            Node = GraphPrototype.Nodes[edge.Target];

            // Perform node actions!
            foreach (var action in Node.Actions)
            {
                await action.PerformAction(Owner, user);

                if (Owner.Deleted)
                    return false;
            }

            if (Target == Node)
                ClearTarget();

            foreach (var completed in edge.Completed)
            {
                await completed.PerformAction(Owner, user);
                if (Owner.Deleted) return true;
            }

            await HandleEntityChange(Node, user);

            return true;
        }

        public void ResetEdge()
        {
            _edgeNestedStepProgress = null;
            TargetNextEdge = null;
            Edge = null;
            EdgeStep = 0;

            UpdateTarget();
        }

        private async Task<bool> HandleEdge(InteractUsingEventArgs eventArgs)
        {
            if (Edge == null || EdgeStep >= Edge.Steps.Count) return false;

            return await HandleStep(eventArgs, Edge, Edge.Steps[EdgeStep]);
        }

        private async Task<bool> HandleEntityChange(ConstructionGraphNode node, IEntity? user = null)
        {
            if (node.Entity == Owner.Prototype?.ID || string.IsNullOrEmpty(node.Entity)
            || GraphPrototype == null) return false;

            var entity = Owner.EntityManager.SpawnEntity(node.Entity, Owner.Transform.Coordinates);

            entity.Transform.LocalRotation = Owner.Transform.LocalRotation;

            if (entity.TryGetComponent(out ConstructionComponent? construction))
            {
                if(construction.GraphPrototype != GraphPrototype)
                    throw new Exception($"New entity {node.Entity}'s graph {construction.GraphPrototype?.ID ?? null} isn't the same as our graph {GraphPrototype.ID} on node {node.Name}!");

                construction.Node = node;
                construction.Target = Target;
                construction._containers = new HashSet<string>(_containers);
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

            if (Owner.TryGetComponent(out IPhysicsComponent? physics) &&
                entity.TryGetComponent(out IPhysicsComponent? otherPhysics))
            {
                otherPhysics.Anchored = physics.Anchored;
            }

            Owner.Delete();

            foreach (var action in node.Actions)
            {
                await action.PerformAction(entity, user);

                if (entity.Deleted)
                    return false;
            }

            return true;
        }

        public bool AddContainer(string id)
        {
            return _containers.Add(id);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (string.IsNullOrEmpty(_graphIdentifier))
            {
                Logger.Error($"Prototype {Owner.Prototype?.ID}'s construction component didn't have a graph identifier!");
                return;
            }

            if (_prototypeManager.TryIndex(_graphIdentifier, out ConstructionGraphPrototype graph))
            {
                GraphPrototype = graph;

                if (GraphPrototype.Nodes.TryGetValue(_startingNodeIdentifier, out var node))
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

        protected override void Startup()
        {
            base.Startup();

            if (Node == null) return;

            foreach (var action in Node.Actions)
            {
                action.PerformAction(Owner, null);

                if (Owner.Deleted)
                    return;
            }
        }

        public async Task ChangeNode(string node)
        {
            if (GraphPrototype == null) return;

            var graphNode = GraphPrototype.Nodes[node];

            if (_handling && _handlingTask?.Task != null)
                await _handlingTask.Task;

            Edge = null;
            Node = graphNode;

            foreach (var action in Node.Actions)
            {
                await action.PerformAction(Owner, null);
                if (Owner.Deleted)
                    return;
            }

            await HandleEntityChange(graphNode);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if(Target != null)
                message.AddMarkup(Loc.GetString("To create {0}...\n", Target.Name));

            if (Edge == null && TargetNextEdge != null)
            {
                var preventStepExamine = false;

                foreach (var condition in TargetNextEdge.Conditions)
                {
                    preventStepExamine |= condition.DoExamine(Owner, message, inDetailsRange);
                }

                if(!preventStepExamine)
                    TargetNextEdge.Steps[0].DoExamine(message, inDetailsRange);
                return;
            }

            if (Edge != null)
            {
                var preventStepExamine = false;

                foreach (var condition in Edge.Conditions)
                {
                    preventStepExamine |= condition.DoExamine(Owner, message, inDetailsRange);
                }

                if (preventStepExamine) return;
            }

            if (_edgeNestedStepProgress == null)
            {
                if(EdgeStep < Edge?.Steps.Count)
                    Edge.Steps[EdgeStep].DoExamine(message, inDetailsRange);
                return;
            }

            foreach (var list in _edgeNestedStepProgress)
            {
                if(list.Count == 0) continue;

                list[0].DoExamine(message, inDetailsRange);
            }
        }
    }
}
