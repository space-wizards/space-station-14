using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Construction.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Construction
{
    public sealed partial class ConstructionSystem
    {
        [Dependency] private readonly IComponentFactory _factory = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

        // --- WARNING! LEGACY CODE AHEAD! ---
        // This entire file contains the legacy code for initial construction.
        // This is bound to be replaced by a better alternative (probably using dummy entities)
        // but for now I've isolated them in their own little file. This code is largely unchanged.
        // --- YOU HAVE BEEN WARNED! AAAH! ---

        private readonly Dictionary<ICommonSession, HashSet<int>> _beingBuilt = new();

        private void InitializeInitial()
        {
            SubscribeNetworkEvent<TryStartStructureConstructionMessage>(HandleStartStructureConstruction);
            SubscribeNetworkEvent<TryStartItemConstructionMessage>(HandleStartItemConstruction);
        }

        // LEGACY CODE. See warning at the top of the file!
        private IEnumerable<EntityUid> EnumerateNearby(EntityUid user)
        {
            foreach (var item in _handsSystem.EnumerateHeld(user))
            {
                if (TryComp(item, out StorageComponent? storage))
                {
                    foreach (var storedEntity in storage.Container.ContainedEntities!)
                    {
                        yield return storedEntity;
                    }
                }

                yield return item;
            }

            if (_inventorySystem.TryGetContainerSlotEnumerator(user, out var containerSlotEnumerator))
            {
                while (containerSlotEnumerator.MoveNext(out var containerSlot))
                {
                    if(!containerSlot.ContainedEntity.HasValue)
                        continue;

                    if (EntityManager.TryGetComponent(containerSlot.ContainedEntity.Value, out StorageComponent? storage))
                    {
                        foreach (var storedEntity in storage.Container.ContainedEntities)
                        {
                            yield return storedEntity;
                        }
                    }

                    yield return containerSlot.ContainedEntity.Value;
                }
            }

            var pos = Transform(user).MapPosition;

            foreach (var near in _lookupSystem.GetEntitiesInRange(pos, 2f, LookupFlags.Contained | LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
            {
                if (near == user)
                    continue;
                if (_interactionSystem.InRangeUnobstructed(pos, near, 2f) && _container.IsInSameOrParentContainer(user, near))
                    yield return near;
            }
        }

        // LEGACY CODE. See warning at the top of the file!
        private async Task<EntityUid?> Construct(EntityUid user, string materialContainer, ConstructionGraphPrototype graph, ConstructionGraphEdge edge, ConstructionGraphNode targetNode)
        {
            // We need a place to hold our construction items!
            var container = _container.EnsureContainer<Container>(user, materialContainer, out var existed);

            if (existed)
            {
                _popup.PopupEntity(Loc.GetString("construction-system-construct-cannot-start-another-construction"), user, user);
                return null;
            }

            var containers = new Dictionary<string, Container>();

            var doAfterTime = 0f;

            // HOLY SHIT THIS IS SOME HACKY CODE.
            // But I'd rather do this shit than risk having collisions with other containers.
            Container GetContainer(string name)
            {
                if (containers.TryGetValue(name, out var container1))
                    return container1;

                while (true)
                {
                    var random = _robustRandom.Next();
                    var c = _container.EnsureContainer<Container>(user, random.ToString(), out var exists);

                    if (exists)
                        continue;

                    containers[name] = c;
                    return c;
                }
            }

            void FailCleanup()
            {
                foreach (var entity in container.ContainedEntities.ToArray())
                {
                    container.Remove(entity);
                }

                foreach (var cont in containers.Values)
                {
                    foreach (var entity in cont.ContainedEntities.ToArray())
                    {
                        cont.Remove(entity);
                    }
                }

                // If we don't do this, items are invisible for some fucking reason. Nice.
                Timer.Spawn(1, ShutdownContainers);
            }

            void ShutdownContainers()
            {
                container.Shutdown();
                foreach (var c in containers.Values.ToArray())
                {
                    c.Shutdown();
                }
            }

            var failed = false;

            var steps = new List<ConstructionGraphStep>();
            var used = new HashSet<EntityUid>();

            foreach (var step in edge.Steps)
            {
                doAfterTime += step.DoAfter;

                var handled = false;

                switch (step)
                {
                    case MaterialConstructionGraphStep materialStep:
                        foreach (var entity in EnumerateNearby(user))
                        {
                            if (!materialStep.EntityValid(entity, out var stack))
                                continue;

                            if (used.Contains(entity))
                                continue;

                            // TODO allow taking from several stacks.
                            // Also update crafting steps to check if it works.
                            var splitStack = _stackSystem.Split(entity, materialStep.Amount, user.ToCoordinates(0, 0), stack);

                            if (splitStack == null)
                                continue;

                            if (string.IsNullOrEmpty(materialStep.Store))
                            {
                                if (!container.Insert(splitStack.Value))
                                    continue;
                            }
                            else if (!GetContainer(materialStep.Store).Insert(splitStack.Value))
                                continue;

                            handled = true;
                            break;
                        }

                        break;

                    case ArbitraryInsertConstructionGraphStep arbitraryStep:
                        foreach (var entity in new HashSet<EntityUid>(EnumerateNearby(user)))
                        {
                            if (!arbitraryStep.EntityValid(entity, EntityManager, _factory))
                                continue;

                            if (used.Contains(entity))
                                continue;

                            // Dump out any stored entities in used entity
                            if (TryComp<StorageComponent>(entity, out var storage))
                            {
                                _container.EmptyContainer(storage.Container);
                            }

                            if (string.IsNullOrEmpty(arbitraryStep.Store))
                            {
                                if (!container.Insert(entity))
                                    continue;
                            }
                            else if (!GetContainer(arbitraryStep.Store).Insert(entity))
                                continue;

                            handled = true;
                            used.Add(entity);
                            break;
                        }

                        break;
                }

                if (handled == false)
                {
                    failed = true;
                    break;
                }

                steps.Add(step);
            }

            if (failed)
            {
                _popup.PopupEntity(Loc.GetString("construction-system-construct-no-materials"), user, user);
                FailCleanup();
                return null;
            }

            var doAfterArgs = new DoAfterArgs(EntityManager, user, doAfterTime, new AwaitedDoAfterEvent(), null)
            {
                BreakOnDamage = true,
                BreakOnTargetMove = false,
                BreakOnUserMove = true,
                NeedHand = false,
                // allow simultaneously starting several construction jobs using the same stack of materials.
                CancelDuplicate = false,
                BlockDuplicate = false,
            };

            if (await _doAfterSystem.WaitDoAfter(doAfterArgs) == DoAfterStatus.Cancelled)
            {
                FailCleanup();
                return null;
            }

            var newEntityProto = graph.Nodes[edge.Target].Entity.GetId(null, user, new(EntityManager));
            var newEntity = EntityManager.SpawnEntity(newEntityProto, EntityManager.GetComponent<TransformComponent>(user).Coordinates);

            if (!TryComp(newEntity, out ConstructionComponent? construction))
            {
                _sawmill.Error($"Initial construction does not have a valid target entity! It is missing a ConstructionComponent.\nGraph: {graph.ID}, Initial Target: {edge.Target}, Ent. Prototype: {newEntityProto}\nCreated Entity {ToPrettyString(newEntity)} will be deleted.");
                Del(newEntity); // Screw you, make proper construction graphs.
                return null;
            }

            // We attempt to set the pathfinding target.
            SetPathfindingTarget(newEntity, targetNode.Name, construction);

            // We preserve the containers...
            foreach (var (name, cont) in containers)
            {
                var newCont = _container.EnsureContainer<Container>(newEntity, name);

                foreach (var entity in cont.ContainedEntities.ToArray())
                {
                    cont.ForceRemove(entity);
                    newCont.Insert(entity);
                }
            }

            // We now get rid of all them.
            ShutdownContainers();

            // We have step completed steps!
            foreach (var step in steps)
            {
                foreach (var completed in step.Completed)
                {
                    completed.PerformAction(newEntity, user, EntityManager);
                }
            }

            // And we also have edge completed effects!
            foreach (var completed in edge.Completed)
            {
                completed.PerformAction(newEntity, user, EntityManager);
            }

            return newEntity;
        }

        private async void HandleStartItemConstruction(TryStartItemConstructionMessage ev, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is {Valid: true} user)
                await TryStartItemConstruction(ev.PrototypeName, user);
        }

        // LEGACY CODE. See warning at the top of the file!
        public async Task<bool> TryStartItemConstruction(string prototype, EntityUid user)
        {
            if (!_prototypeManager.TryIndex(prototype, out ConstructionPrototype? constructionPrototype))
            {
                _sawmill.Error($"Tried to start construction of invalid recipe '{prototype}'!");
                return false;
            }

            if (!_prototypeManager.TryIndex(constructionPrototype.Graph,
                    out ConstructionGraphPrototype? constructionGraph))
            {
                _sawmill.Error(
                    $"Invalid construction graph '{constructionPrototype.Graph}' in recipe '{prototype}'!");
                return false;
            }

            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];
            var pathFind = constructionGraph.Path(startNode.Name, targetNode.Name);

            if (!_actionBlocker.CanInteract(user, null))
                return false;

            if (!HasComp<HandsComponent>(user))
                return false;

            foreach (var condition in constructionPrototype.Conditions)
            {
                if (!condition.Condition(user, user.ToCoordinates(0, 0), Direction.South))
                    return false;
            }

            if (pathFind == null)
            {
                throw new InvalidDataException(
                    $"Can't find path from starting node to target node in construction! Recipe: {prototype}");
            }

            var edge = startNode.GetEdge(pathFind[0].Name);

            if (edge == null)
            {
                throw new InvalidDataException(
                    $"Can't find edge from starting node to the next node in pathfinding! Recipe: {prototype}");
            }

            // No support for conditions here!

            foreach (var step in edge.Steps)
            {
                switch (step)
                {
                    case ToolConstructionGraphStep _:
                        throw new InvalidDataException("Invalid first step for construction recipe!");
                }
            }

            if (await Construct(user, "item_construction", constructionGraph, edge, targetNode) is not { Valid: true } item)
                return false;

            // Just in case this is a stack, attempt to merge it. If it isn't a stack, this will just normally pick up
            // or drop the item as normal.
            _stackSystem.TryMergeToHands(item, user);
            return true;
        }

        // LEGACY CODE. See warning at the top of the file!
        private async void HandleStartStructureConstruction(TryStartStructureConstructionMessage ev, EntitySessionEventArgs args)
        {

            if (!_prototypeManager.TryIndex(ev.PrototypeName, out ConstructionPrototype? constructionPrototype))
            {
                _sawmill.Error($"Tried to start construction of invalid recipe '{ev.PrototypeName}'!");
                RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));
                return;
            }

            if (!_prototypeManager.TryIndex(constructionPrototype.Graph, out ConstructionGraphPrototype? constructionGraph))
            {
                _sawmill.Error($"Invalid construction graph '{constructionPrototype.Graph}' in recipe '{ev.PrototypeName}'!");
                RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));
                return;
            }

            if (args.SenderSession.AttachedEntity is not {Valid: true} user)
            {
                _sawmill.Error($"Client sent {nameof(TryStartStructureConstructionMessage)} with no attached entity!");
                return;
            }

            if (_container.IsEntityInContainer(user))
            {
                _popup.PopupEntity(Loc.GetString("construction-system-inside-container"), user, user);
                return;
            }

            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];
            var pathFind = constructionGraph.Path(startNode.Name, targetNode.Name);


            if (_beingBuilt.TryGetValue(args.SenderSession, out var set))
            {
                if (!set.Add(ev.Ack))
                {
                    _popup.PopupEntity(Loc.GetString("construction-system-already-building"), user, user);
                    return;
                }
            }
            else
            {
                var newSet = new HashSet<int> {ev.Ack};
                _beingBuilt[args.SenderSession] = newSet;
            }

            var location = GetCoordinates(ev.Location);

            foreach (var condition in constructionPrototype.Conditions)
            {
                if (!condition.Condition(user, location, ev.Angle.GetCardinalDir()))
                {
                    Cleanup();
                    return;
                }
            }

            void Cleanup()
            {
                _beingBuilt[args.SenderSession].Remove(ev.Ack);
            }

            if (!_actionBlocker.CanInteract(user, null)
                || !EntityManager.TryGetComponent(user, out HandsComponent? hands) || hands.ActiveHandEntity == null)
            {
                Cleanup();
                return;
            }

            var mapPos = location.ToMap(EntityManager);
            var predicate = GetPredicate(constructionPrototype.CanBuildInImpassable, mapPos);

            if (!_interactionSystem.InRangeUnobstructed(user, mapPos, predicate: predicate))
            {
                Cleanup();
                return;
            }

            if (pathFind == null)
                throw new InvalidDataException($"Can't find path from starting node to target node in construction! Recipe: {ev.PrototypeName}");

            var edge = startNode.GetEdge(pathFind[0].Name);

            if(edge == null)
                throw new InvalidDataException($"Can't find edge from starting node to the next node in pathfinding! Recipe: {ev.PrototypeName}");

            var valid = false;

            if (hands.ActiveHandEntity is not {Valid: true} holding)
            {
                Cleanup();
                return;
            }

            // No support for conditions here!

            foreach (var step in edge.Steps)
            {
                switch (step)
                {
                    case EntityInsertConstructionGraphStep entityInsert:
                        if (entityInsert.EntityValid(holding, EntityManager, _factory))
                            valid = true;
                        break;
                    case ToolConstructionGraphStep _:
                        throw new InvalidDataException("Invalid first step for item recipe!");
                }

                if (valid)
                    break;
            }

            if (!valid)
            {
                Cleanup();
                return;
            }

            if (await Construct(user, (ev.Ack + constructionPrototype.GetHashCode()).ToString(), constructionGraph,
                    edge, targetNode) is not {Valid: true} structure)
            {
                Cleanup();
                return;
            }

            // We do this to be able to move the construction to its proper position in case it's anchored...
            // Oh wow transform anchoring is amazing wow I love it!!!!
            // ikr
            var xform = Transform(structure);
            var wasAnchored = xform.Anchored;
            xform.Anchored = false;
            xform.Coordinates = GetCoordinates(ev.Location);
            xform.LocalRotation = constructionPrototype.CanRotate ? ev.Angle : Angle.Zero;
            xform.Anchored = wasAnchored;

            RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack, GetNetEntity(structure)));
            _adminLogger.Add(LogType.Construction, LogImpact.Low, $"{ToPrettyString(user):player} has turned a {ev.PrototypeName} construction ghost into {ToPrettyString(structure)} at {Transform(structure).Coordinates}");
            Cleanup();
        }
    }
}
