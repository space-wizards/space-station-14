using Content.Server.Construction.Components;
using Content.Server.Containers;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Containers;
using Content.Shared.Database;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction
{
    public sealed partial class ConstructionSystem
    {
        private void InitializeGraphs()
        {
        }

        /// <summary>
        ///     Sets a container on an entity as being handled by Construction. This essentially means that it will
        ///     be transferred if the entity prototype changes. <seealso cref="ChangeEntity"/>
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="container">The container identifier. This method does not check whether the container exists.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>Whether we could set the container as being handled by construction or not. Also returns false if
        ///          the entity does not have a <see cref="ConstructionComponent"/>.</returns>
        public bool AddContainer(EntityUid uid, string container, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            return construction.Containers.Add(container);
        }

        /// <summary>
        ///     Gets the current construction graph of an entity, or null.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The current construction graph of an entity or null if invalid. Also returns null if the entity
        ///          does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>An entity with a valid construction state will always have a valid graph.</remarks>
        public ConstructionGraphPrototype? GetCurrentGraph(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            // If the set graph prototype does not exist, also return null. This could be due to admemes changing values
            // in ViewVariables, so even though the construction state is invalid, just return null.
            return _prototypeManager.TryIndex(construction.Graph, out ConstructionGraphPrototype? graph) ? graph : null;
        }

        /// <summary>
        ///     Gets the construction graph node the entity is currently at, or null.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The current construction graph node the entity is currently at, or null if invalid. Also returns
        ///          null if the entity does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>An entity with a valid construction state will always be at a valid node.</remarks>
        public ConstructionGraphNode? GetCurrentNode(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (construction.Node is not {} nodeIdentifier)
                return null;

            return GetCurrentGraph(uid, construction) is not {} graph ? null : GetNodeFromGraph(graph, nodeIdentifier);
        }

        /// <summary>
        ///     Gets the construction graph edge the entity is currently at, or null.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The construction graph edge the entity is currently at, if any. Also returns null if the entity
        ///          does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>An entity with a valid construction state might not always be at an edge.</remarks>
        public ConstructionGraphEdge? GetCurrentEdge(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (construction.EdgeIndex is not {} edgeIndex)
                return null;

            return GetCurrentNode(uid, construction) is not {} node ? null : GetEdgeFromNode(node, edgeIndex);
        }

        /// <summary>
        ///     Variant of <see cref="GetCurrentEdge"/> that returns both the node and edge.
        /// </summary>
        public (ConstructionGraphNode?, ConstructionGraphEdge?) GetCurrentNodeAndEdge(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return (null, null);

            if (GetCurrentNode(uid, construction) is not { } node)
                return (null, null);

            if (construction.EdgeIndex is not {} edgeIndex)
                return (node, null);

            return (node, GetEdgeFromNode(node, edgeIndex));
        }

        /// <summary>
        ///     Gets the construction graph step the entity is currently at, or null.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The construction graph step the entity is currently at, if any. Also returns null if the entity
        ///          does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>An entity with a valid construction state might not always be at a step or an edge.</remarks>
        public ConstructionGraphStep? GetCurrentStep(EntityUid uid, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return null;

            if (GetCurrentEdge(uid, construction) is not {} edge)
                return null;

            return GetStepFromEdge(edge, construction.StepIndex);
        }

        /// <summary>
        ///     Gets the construction graph node the entity's construction pathfinding is currently targeting, if any.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The construction graph node the entity's construction pathfinding is currently targeting, or null
        ///          if it's not currently targeting any node. Also returns null if the entity does not have a
        ///          <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>Target nodes are entirely optional and only used for pathfinding purposes.</remarks>
        public ConstructionGraphNode? GetTargetNode(EntityUid uid, ConstructionComponent? construction)
        {
            if (!Resolve(uid, ref construction))
                return null;

            if (construction.TargetNode is not {} targetNodeId)
                return null;

            if (GetCurrentGraph(uid, construction) is not {} graph)
                return null;

            return GetNodeFromGraph(graph, targetNodeId);
        }

        /// <summary>
        ///     Gets the construction graph edge the entity's construction pathfinding is currently targeting, if any.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>The construction graph edge the entity's construction pathfinding is currently targeting, or null
        ///          if it's not currently targeting any edge. Also returns null if the entity does not have a
        ///          <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>Target edges are entirely optional and only used for pathfinding purposes. The targeted edge will
        ///          be an edge on the current construction node the entity is at.</remarks>
        public ConstructionGraphEdge? GetTargetEdge(EntityUid uid, ConstructionComponent? construction)
        {
            if (!Resolve(uid, ref construction))
                return null;

            if (construction.TargetEdgeIndex is not {} targetEdgeIndex)
                return null;

            if (GetCurrentNode(uid, construction) is not {} node)
                return null;

            return GetEdgeFromNode(node, targetEdgeIndex);
        }

        /// <summary>
        ///     Gets both the construction edge and step the entity is currently at, if any.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>A tuple containing the current edge and step the entity's construction state is at.</returns>
        /// <remarks>The edge, step or both could be null. A valid construction state does not necessarily need them.</remarks>
        public (ConstructionGraphEdge? edge, ConstructionGraphStep? step) GetCurrentEdgeAndStep(EntityUid uid,
            ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction, false))
                return default;

            var edge = GetCurrentEdge(uid, construction);

            if (edge == null)
                return default;

            var step = GetStepFromEdge(edge, construction.StepIndex);

            return (edge, step);
        }

        /// <summary>
        ///     Gets a node from a construction graph given its identifier.
        /// </summary>
        /// <param name="graph">The construction graph where to get the node.</param>
        /// <param name="id">The identifier that corresponds to the node.</param>
        /// <returns>The node that corresponds to the identifier, or null if it doesn't exist.</returns>
        public ConstructionGraphNode? GetNodeFromGraph(ConstructionGraphPrototype graph, string id)
        {
            return graph.Nodes.TryGetValue(id, out var node) ? node : null;
        }

        /// <summary>
        ///     Gets an edge from a construction node given its index.
        /// </summary>
        /// <param name="node">The construction node where to get the edge.</param>
        /// <param name="index">The index or position of the edge on the node.</param>
        /// <returns>The edge on that index in the construction node, or null if none.</returns>
        public ConstructionGraphEdge? GetEdgeFromNode(ConstructionGraphNode node, int index)
        {
            return node.Edges.Count > index ? node.Edges[index] : null;
        }

        /// <summary>
        ///     Gets a step from a construction edge given its index.
        /// </summary>
        /// <param name="edge">The construction edge where to get the step.</param>
        /// <param name="index">The index or position of the step on the edge.</param>
        /// <returns>The edge on that index in the construction edge, or null if none.</returns>
        public ConstructionGraphStep? GetStepFromEdge(ConstructionGraphEdge edge, int index)
        {
            return edge.Steps.Count > index ? edge.Steps[index] : null;
        }

        /// <summary>
        ///     Performs a node change on a construction entity, optionally performing the actions for the new node.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="userUid">An optional user entity, for actions.</param>
        /// <param name="id">The identifier of the node to change to.</param>
        /// <param name="performActions">Whether the actions for the new node will be performed or not.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>Whether the node change succeeded or not. Also returns false if the entity does not have a <see cref="ConstructionComponent"/>.</returns>
        /// <remarks>This method also updates the construction pathfinding automatically, if the node change succeeds.</remarks>
        public bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            if (GetCurrentGraph(uid, construction) is not {} graph
            ||  GetNodeFromGraph(graph, id) is not {} node)
                return false;

            var oldNode = construction.Node;
            construction.Node = id;

            if (userUid != null)
                _adminLogger.Add(LogType.Construction, LogImpact.Low,
                    $"{ToPrettyString(userUid.Value):player} changed {ToPrettyString(uid):entity}'s node from \"{oldNode}\" to \"{id}\"");

            // ChangeEntity will handle the pathfinding update.
            if (node.Entity.GetId(uid, userUid, new(EntityManager)) is {} newEntity
                && ChangeEntity(uid, userUid, newEntity, construction) != null)
                return true;

            if(performActions)
                PerformActions(uid, userUid, node.Actions);

            // An action might have deleted the entity... Account for this.
            if (!Exists(uid))
                return false;

            UpdatePathfinding(uid, construction);
            return true;
        }

        /// <summary>
        ///     Performs an entity prototype change on a construction entity.
        ///     The old entity will be removed, and a new one will be spawned in its place. Some values will be kept,
        ///     and any containers handled by construction will be transferred to the new entity as well.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="userUid">An optional user entity, for actions.</param>
        /// <param name="newEntity">The entity prototype identifier for the new entity.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <param name="metaData">The metadata component of the target entity. Will be resolved if null.</param>
        /// <param name="transform">The transform component of the target entity. Will be resolved if null.</param>
        /// <param name="containerManager">The container manager component of the target entity. Will be resolved if null,
        ///                                but it is an optional component and not required for the method to work.</param>
        /// <returns>The new entity, or null if the method did not succeed.</returns>
        private EntityUid? ChangeEntity(EntityUid uid, EntityUid? userUid, string newEntity,
            ConstructionComponent? construction = null,
            MetaDataComponent? metaData = null,
            TransformComponent? transform = null,
            ContainerManagerComponent? containerManager = null)
        {
            if (!Resolve(uid, ref construction, ref metaData, ref transform))
            {
                // Failed resolve logs an error, but we want to actually log information about the failed construction
                // graph. So lets let the UpdateInteractions() try-catch log that info for us.
                throw new Exception("Missing construction components");
            }

            if (newEntity == metaData.EntityPrototype?.ID || !_prototypeManager.HasIndex<EntityPrototype>(newEntity))
                return null;

            // Optional resolves.
            Resolve(uid, ref containerManager, false);

            // We create the new entity.
            var newUid = EntityManager.CreateEntityUninitialized(newEntity, transform.Coordinates);

            // Construction transferring.
            var newConstruction = EntityManager.EnsureComponent<ConstructionComponent>(newUid);

            // Transfer all construction-owned containers.
            newConstruction.Containers.UnionWith(construction.Containers);

            // Prevent MapInitEvent spawned entities from spawning into the containers.
            // Containers created by ChangeNode() actions do not exist until after this function is complete,
            // but this should be fine, as long as the target entity properly declared its managed containers.
            if (TryComp(newUid, out ContainerFillComponent? containerFill) && containerFill.IgnoreConstructionSpawn)
            {
                foreach (var id in newConstruction.Containers)
                {
                    containerFill.Containers.Remove(id);
                }
            }

            // If the new entity has the *same* construction graph, stay on the same node.
            // If not, we effectively restart the construction graph, so the new entity can be completed.
            if (construction.Graph == newConstruction.Graph)
            {
                ChangeNode(newUid, userUid, construction.Node, false, newConstruction);

                // Retain the target node if an entity change happens in response to deconstruction;
                // in that case, we must continue to move towards the start node.
                if (construction.TargetNode is {} targetNode)
                    SetPathfindingTarget(newUid, targetNode, newConstruction);
            }

            // Transfer all pending interaction events too.
            while (construction.InteractionQueue.TryDequeue(out var ev))
            {
                newConstruction.InteractionQueue.Enqueue(ev);
            }

            if (newConstruction.InteractionQueue.Count > 0 && _queuedUpdates.Add(newUid))
                    _constructionUpdateQueue.Enqueue(newUid);

            // Transform transferring.
            var newTransform = Transform(newUid);
            newTransform.AttachToGridOrMap(); // in case in hands or a container
            newTransform.LocalRotation = transform.LocalRotation;
            newTransform.Anchored = transform.Anchored;

            // Container transferring.
            if (containerManager != null)
            {
                // Ensure the new entity has a container manager. Also for resolve goodness.
                var newContainerManager = EntityManager.EnsureComponent<ContainerManagerComponent>(newUid);

                // Transfer all construction-owned containers from the old entity to the new one.
                foreach (var container in construction.Containers)
                {
                    if (!_container.TryGetContainer(uid, container, out var ourContainer, containerManager))
                        continue;

                    if (!_container.TryGetContainer(newUid, container, out var otherContainer, newContainerManager))
                    {
                        // NOTE: Only Container is supported by Construction!
                        // todo: one day, the ensured container should be the same type as ourContainer
                        otherContainer = _container.EnsureContainer<Container>(newUid, container, newContainerManager);
                    }

                    for (var i = ourContainer.ContainedEntities.Count - 1; i >= 0; i--)
                    {
                        var entity = ourContainer.ContainedEntities[i];
                        ourContainer.ForceRemove(entity);
                        otherContainer.Insert(entity);
                    }
                }
            }

            var entChangeEv = new ConstructionChangeEntityEvent(newUid, uid);
            RaiseLocalEvent(uid, entChangeEv);
            RaiseLocalEvent(newUid, entChangeEv, broadcast: true);

            foreach (var logic in GetCurrentNode(newUid, newConstruction)!.TransformLogic)
            {
                logic.Transform(uid, newUid, userUid, new(EntityManager));
            }

            EntityManager.InitializeAndStartEntity(newUid);

            QueueDel(uid);

            return newUid;
        }

        /// <summary>
        ///     Performs a construction graph change on a construction entity, also changing the node to a valid one on
        ///     the new graph.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="userUid">An optional user entity, for actions.</param>
        /// <param name="graphId">The identifier for the construction graph to switch to.</param>
        /// <param name="nodeId">The identifier for a node on the new construction graph to switch to.</param>
        /// <param name="performActions">Whether actions on the new node will be performed or not.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>Whether the construction graph change succeeded or not. Returns false if the entity does not have
        ///          a <see cref="ConstructionComponent"/>.</returns>
        public bool ChangeGraph(EntityUid uid, EntityUid? userUid, string graphId, string nodeId, bool performActions = true, ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            if (!_prototypeManager.TryIndex<ConstructionGraphPrototype>(graphId, out var graph))
                return false;

            if(GetNodeFromGraph(graph, nodeId) is not {})
                return false;

            construction.Graph = graphId;
            return ChangeNode(uid, userUid, nodeId, performActions, construction);
        }
    }

    /// <summary>
    ///     This event gets raised when an entity changes prototype / uid during construction. The event is raised
    ///     directed both at the old and new entity.
    /// </summary>
    public sealed class ConstructionChangeEntityEvent : EntityEventArgs
    {
        public readonly EntityUid New;
        public readonly EntityUid Old;

        public ConstructionChangeEntityEvent(EntityUid newUid, EntityUid oldUid)
        {
            New = newUid;
            Old = oldUid;
        }
    }
}
