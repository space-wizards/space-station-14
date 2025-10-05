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
using System.Linq;
using Content.Shared.Construction.Components;

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
        ///          the entity does not have a <see cref="Shared.Construction.Components.ConstructionComponent"/>.</returns>
        public bool AddContainer(EntityUid uid, string container, Shared.Construction.Components.ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            return construction.Containers.Add(container);
        }

        /// <summary>
        ///     Performs a node change on a construction entity, optionally performing the actions for the new node.
        /// </summary>
        /// <param name="uid">The target entity.</param>
        /// <param name="userUid">An optional user entity, for actions.</param>
        /// <param name="id">The identifier of the node to change to.</param>
        /// <param name="performActions">Whether the actions for the new node will be performed or not.</param>
        /// <param name="construction">The construction component of the target entity. Will be resolved if null.</param>
        /// <returns>Whether the node change succeeded or not. Also returns false if the entity does not have a <see cref="Shared.Construction.Components.ConstructionComponent"/>.</returns>
        /// <remarks>This method also updates the construction pathfinding automatically, if the node change succeeds.</remarks>
        public bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true, Shared.Construction.Components.ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            if (GetCurrentGraph(uid, construction) is not { } graph
            || GetNodeFromGraph(graph, id) is not { } node)
                return false;

            var oldNode = construction.Node;
            construction.Node = id;

            if (userUid != null)
                _adminLogger.Add(LogType.Construction, LogImpact.Low,
                    $"{ToPrettyString(userUid.Value):player} changed {ToPrettyString(uid):entity}'s node from \"{oldNode}\" to \"{id}\"");

            // ChangeEntity will handle the pathfinding update.
            if (node.Entity.GetId(uid, userUid, new(EntityManager)) is { } newEntity
                && ChangeEntity(uid, userUid, newEntity, construction, oldNode) != null)
                return true;

            if (performActions)
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
        /// <param name="previousNode">The previous node, if any, this graph was on before changing entity.</param>
        /// <param name="metaData">The metadata component of the target entity. Will be resolved if null.</param>
        /// <param name="transform">The transform component of the target entity. Will be resolved if null.</param>
        /// <param name="containerManager">The container manager component of the target entity. Will be resolved if null,
        ///                                but it is an optional component and not required for the method to work.</param>
        /// <returns>The new entity, or null if the method did not succeed.</returns>
        private EntityUid? ChangeEntity(EntityUid uid, EntityUid? userUid, string newEntity,
            Shared.Construction.Components.ConstructionComponent? construction = null,
            string? previousNode = null,
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

            // Exit if the new entity's prototype is the same as the original, or the prototype is invalid
            if (newEntity == metaData.EntityPrototype?.ID || !PrototypeManager.HasIndex<EntityPrototype>(newEntity))
                return null;

            // [Optional] Exit if the new entity's prototype is a parent of the original
            // E.g., if an entity with the 'AirlockCommand' prototype was to be replaced with a new entity that
            // had the 'Airlock' prototype, and DoNotReplaceInheritingEntities was true, the code block would
            // exit here because 'AirlockCommand' is derived from 'Airlock'
            if (GetCurrentNode(uid, construction)?.DoNotReplaceInheritingEntities == true &&
                metaData.EntityPrototype?.ID != null)
            {
                var parents = PrototypeManager.EnumerateParents<EntityPrototype>(metaData.EntityPrototype.ID)?.ToList();

                if (parents != null && parents.Any(x => x.ID == newEntity))
                    return null;
            }

            // Optional resolves.
            Resolve(uid, ref containerManager, false);

            // We create the new entity.
            var newUid = EntityManager.CreateEntityUninitialized(newEntity, transform.Coordinates);

            // Construction transferring.
            var newConstruction = EnsureComp<Shared.Construction.Components.ConstructionComponent>(newUid);

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
                if (construction.TargetNode is { } targetNode)
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
            TransformSystem.AttachToGridOrMap(newUid, newTransform); // in case in hands or a container
            newTransform.LocalRotation = transform.LocalRotation;
            newTransform.Anchored = transform.Anchored;

            // Container transferring.
            if (containerManager != null)
            {
                // Ensure the new entity has a container manager. Also for resolve goodness.
                var newContainerManager = EnsureComp<ContainerManagerComponent>(newUid);

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
                        _container.Remove(entity, ourContainer, reparent: false, force: true);
                        _container.Insert(entity, otherContainer);
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

            // If ChangeEntity has ran, then the entity uid has changed and the
            // new entity should be initialized by this point.
            var afterChangeEv = new AfterConstructionChangeEntityEvent(construction.Graph, construction.Node, previousNode);
            RaiseLocalEvent(newUid, ref afterChangeEv);

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
        ///          a <see cref="Shared.Construction.Components.ConstructionComponent"/>.</returns>
        public bool ChangeGraph(EntityUid uid, EntityUid? userUid, string graphId, string nodeId, bool performActions = true, Shared.Construction.Components.ConstructionComponent? construction = null)
        {
            if (!Resolve(uid, ref construction))
                return false;

            if (!PrototypeManager.TryIndex<ConstructionGraphPrototype>(graphId, out var graph))
                return false;

            if (GetNodeFromGraph(graph, nodeId) is not { })
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

    /// <summary>
    /// This event is raised after an entity changes prototype/uid during construction.
    /// This is only raised at the new entity, after it has been initialized.
    /// </summary>
    /// <param name="Graph">Construction graph for this entity.</param>
    /// <param name="CurrentNode">New node that has become active.</param>
    /// <param name="PreviousNode">Previous node that was active on the graph.</param>
    [ByRefEvent]
    public record struct AfterConstructionChangeEntityEvent(string Graph, string CurrentNode, string? PreviousNode)
    {
    }
}
