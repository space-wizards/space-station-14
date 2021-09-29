using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.Stack;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Construction
{
    /// <summary>
    /// The server-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    internal class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;

        private readonly Dictionary<ICommonSession, HashSet<int>> _beingBuilt = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<TryStartStructureConstructionMessage>(HandleStartStructureConstruction);
            SubscribeNetworkEvent<TryStartItemConstructionMessage>(HandleStartItemConstruction);
            SubscribeLocalEvent<ConstructionComponent, ExaminedEvent>(HandleConstructionExamined);
        }

        private void HandleConstructionExamined(EntityUid uid, ConstructionComponent component, ExaminedEvent args)
        {
            if (component.Target != null)
            {
                args.PushMarkup(Loc.GetString(
                    "construction-component-to-create-header",
                    ("targetName", component.Target.Name)));
            }

            if (component.Edge == null && component.TargetNextEdge != null)
            {
                var preventStepExamine = false;

                foreach (var condition in component.TargetNextEdge.Conditions)
                {
                    preventStepExamine |= condition.DoExamine(args);
                }

                if (!preventStepExamine)
                    component.TargetNextEdge.Steps[0].DoExamine(args);
                return;
            }

            if (component.Edge != null)
            {
                var preventStepExamine = false;

                foreach (var condition in component.Edge.Conditions)
                {
                    preventStepExamine |= condition.DoExamine(args);
                }

                if (preventStepExamine) return;
            }

            if (component.EdgeNestedStepProgress == null)
            {
                if (component.EdgeStep < component.Edge?.Steps.Count)
                    component.Edge.Steps[component.EdgeStep].DoExamine(args);
                return;
            }

            foreach (var list in component.EdgeNestedStepProgress)
            {
                if(list.Count == 0) continue;

                list[0].DoExamine(args);
            }

        }

        private IEnumerable<IEntity> EnumerateNearby(IEntity user)
        {
            if (user.TryGetComponent(out HandsComponent? hands))
            {
                foreach (var itemComponent in hands?.GetAllHeldItems()!)
                {
                    if (itemComponent.Owner.TryGetComponent(out ServerStorageComponent? storage))
                    {
                        foreach (var storedEntity in storage.StoredEntities!)
                        {
                            yield return storedEntity;
                        }
                    }

                    yield return itemComponent.Owner;
                }
            }

            if (user!.TryGetComponent(out InventoryComponent? inventory))
            {
                foreach (var held in inventory.GetAllHeldItems())
                {
                    if (held.TryGetComponent(out ServerStorageComponent? storage))
                    {
                        foreach (var storedEntity in storage.StoredEntities!)
                        {
                            yield return storedEntity;
                        }
                    }

                    yield return held;
                }
            }

            foreach (var near in IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(user!, 2f, LookupFlags.Approximate | LookupFlags.IncludeAnchored))
            {
                yield return near;
            }
        }

        private async Task<IEntity?> Construct(IEntity user, string materialContainer, ConstructionGraphPrototype graph, ConstructionGraphEdge edge, ConstructionGraphNode targetNode)
        {
            // We need a place to hold our construction items!
            var container = ContainerHelpers.EnsureContainer<Container>(user, materialContainer, out var existed);

            if (existed)
            {
                user.PopupMessageCursor(Loc.GetString("construction-system-construct-cannot-start-another-construction"));
                return null;
            }

            var containers = new Dictionary<string, Container>();

            var doAfterTime = 0f;

            // HOLY SHIT THIS IS SOME HACKY CODE.
            // But I'd rather do this shit than risk having collisions with other containers.
            Container GetContainer(string name)
            {
                if (containers!.ContainsKey(name))
                    return containers[name];

                while (true)
                {
                    var random = _robustRandom.Next();
                    var c = ContainerHelpers.EnsureContainer<Container>(user!, random.ToString(), out var existed);

                    if (existed) continue;

                    containers[name] = c;
                    return c;
                }
            }

            void FailCleanup()
            {
                foreach (var entity in container!.ContainedEntities.ToArray())
                {
                    container.Remove(entity);
                }

                foreach (var cont in containers!.Values)
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
                container!.Shutdown();
                foreach (var c in containers!.Values.ToArray())
                {
                    c.Shutdown();
                }
            }

            var failed = false;

            var steps = new List<ConstructionGraphStep>();

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

                            var splitStack = _stackSystem.Split(entity.Uid, materialStep.Amount, user.ToCoordinates(), stack);

                            if (splitStack == null)
                                continue;

                            if (string.IsNullOrEmpty(materialStep.Store))
                            {
                                if (!container.Insert(splitStack))
                                    continue;
                            }
                            else if (!GetContainer(materialStep.Store).Insert(splitStack))
                                    continue;

                            handled = true;
                            break;
                        }

                        break;

                    case ArbitraryInsertConstructionGraphStep arbitraryStep:
                        foreach (var entity in EnumerateNearby(user))
                        {
                            if (!arbitraryStep.EntityValid(entity))
                                continue;

                            if (string.IsNullOrEmpty(arbitraryStep.Store))
                            {
                                if (!container.Insert(entity))
                                    continue;
                            }
                            else if (!GetContainer(arbitraryStep.Store).Insert(entity))
                                continue;

                            handled = true;
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
                user.PopupMessageCursor(Loc.GetString("construction-system-construct-no-materials"));
                FailCleanup();
                return null;
            }

            var doAfterArgs = new DoAfterEventArgs(user, doAfterTime)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = false,
                BreakOnUserMove = true,
                NeedHand = false,
            };

            if (await _doAfterSystem.WaitDoAfter(doAfterArgs) == DoAfterStatus.Cancelled)
            {
                FailCleanup();
                return null;
            }

            var newEntity = EntityManager.SpawnEntity(graph.Nodes[edge.Target].Entity, user.Transform.Coordinates);

            // Yes, this should throw if it's missing the component.
            var construction = newEntity.GetComponent<ConstructionComponent>();

            // We attempt to set the pathfinding target.
            construction.Target = targetNode;

            // We preserve the containers...
            foreach (var (name, cont) in containers)
            {
                var newCont = ContainerHelpers.EnsureContainer<Container>(newEntity, name);

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
                    await completed.PerformAction(newEntity, user);
                }
            }

            // And we also have edge completed effects!
            foreach (var completed in edge.Completed)
            {
                await completed.PerformAction(newEntity, user);
            }

            return newEntity;
        }

        private async void HandleStartItemConstruction(TryStartItemConstructionMessage ev, EntitySessionEventArgs args)
        {
            if (!_prototypeManager.TryIndex(ev.PrototypeName, out ConstructionPrototype? constructionPrototype))
            {
                Logger.Error($"Tried to start construction of invalid recipe '{ev.PrototypeName}'!");
                return;
            }

            if (!_prototypeManager.TryIndex(constructionPrototype.Graph, out ConstructionGraphPrototype? constructionGraph))
            {
                Logger.Error($"Invalid construction graph '{constructionPrototype.Graph}' in recipe '{ev.PrototypeName}'!");
                return;
            }

            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];
            var pathFind = constructionGraph.Path(startNode.Name, targetNode.Name);

            var user = args.SenderSession.AttachedEntity;

            if (user == null || !Get<ActionBlockerSystem>().CanInteract(user)) return;

            if (!user.TryGetComponent(out HandsComponent? hands)) return;

            foreach (var condition in constructionPrototype.Conditions)
            {
                if (!condition.Condition(user, user.ToCoordinates(), Direction.South))
                    return;
            }

            if(pathFind == null)
                throw new InvalidDataException($"Can't find path from starting node to target node in construction! Recipe: {ev.PrototypeName}");

            var edge = startNode.GetEdge(pathFind[0].Name);

            if(edge == null)
                throw new InvalidDataException($"Can't find edge from starting node to the next node in pathfinding! Recipe: {ev.PrototypeName}");

            // No support for conditions here!

            foreach (var step in edge.Steps)
            {
                switch (step)
                {
                    case ToolConstructionGraphStep _:
                    case NestedConstructionGraphStep _:
                        throw new InvalidDataException("Invalid first step for construction recipe!");
                }
            }

            var item = await Construct(user, "item_construction", constructionGraph, edge, targetNode);

            if(item != null && item.TryGetComponent(out ItemComponent? itemComp))
                hands.PutInHandOrDrop(itemComp);
        }

        private async void HandleStartStructureConstruction(TryStartStructureConstructionMessage ev, EntitySessionEventArgs args)
        {

            if (!_prototypeManager.TryIndex(ev.PrototypeName, out ConstructionPrototype? constructionPrototype))
            {
                Logger.Error($"Tried to start construction of invalid recipe '{ev.PrototypeName}'!");
                RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));
                return;
            }

            if (!_prototypeManager.TryIndex(constructionPrototype.Graph, out ConstructionGraphPrototype? constructionGraph))
            {
                Logger.Error($"Invalid construction graph '{constructionPrototype.Graph}' in recipe '{ev.PrototypeName}'!");
                RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));
                return;
            }

            var user = args.SenderSession.AttachedEntity;

            if (user == null)
            {
                Logger.Error($"Client sent {nameof(TryStartStructureConstructionMessage)} with no attached entity!");
                return;
            }

            if (user.IsInContainer())
            {
                user.PopupMessageCursor(Loc.GetString("construction-system-inside-container"));
                return;
            }

            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];
            var pathFind = constructionGraph.Path(startNode.Name, targetNode.Name);


            if (_beingBuilt.TryGetValue(args.SenderSession, out var set))
            {
                if (!set.Add(ev.Ack))
                {
                    user.PopupMessageCursor(Loc.GetString("construction-system-already-building"));
                    return;
                }
            }
            else
            {
                var newSet = new HashSet<int> {ev.Ack};
                _beingBuilt[args.SenderSession] = newSet;
            }

            foreach (var condition in constructionPrototype.Conditions)
            {
                if (!condition.Condition(user, ev.Location, ev.Angle.GetCardinalDir()))
                {
                    Cleanup();
                    return;
                }
            }

            void Cleanup()
            {
                _beingBuilt[args.SenderSession].Remove(ev.Ack);
            }

            if (user == null
                || !Get<ActionBlockerSystem>().CanInteract(user)
                || !user.TryGetComponent(out HandsComponent? hands) || hands.GetActiveHand == null
                || !user.InRangeUnobstructed(ev.Location, ignoreInsideBlocker:constructionPrototype.CanBuildInImpassable))
            {
                Cleanup();
                return;
            }

            if(pathFind == null)
                throw new InvalidDataException($"Can't find path from starting node to target node in construction! Recipe: {ev.PrototypeName}");

            var edge = startNode.GetEdge(pathFind[0].Name);

            if(edge == null)
                throw new InvalidDataException($"Can't find edge from starting node to the next node in pathfinding! Recipe: {ev.PrototypeName}");

            var valid = false;
            var holding = hands.GetActiveHand?.Owner;

            if (holding == null)
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
                        if (entityInsert.EntityValid(holding))
                            valid = true;
                        break;
                    case ToolConstructionGraphStep _:
                    case NestedConstructionGraphStep _:
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

            var structure = await Construct(user, (ev.Ack + constructionPrototype.GetHashCode()).ToString(), constructionGraph, edge, targetNode);

            if (structure == null)
            {
                Cleanup();
                return;
            }

            // We do this to be able to move the construction to its proper position in case it's anchored...
            // Oh wow transform anchoring is amazing wow I love it!!!!
            var wasAnchored = structure.Transform.Anchored;
            structure.Transform.Anchored = false;

            structure.Transform.Coordinates = ev.Location;
            structure.Transform.LocalRotation = constructionPrototype.CanRotate ? ev.Angle : Angle.Zero;

            structure.Transform.Anchored = wasAnchored;

            RaiseNetworkEvent(new AckStructureConstructionMessage(ev.Ack));

            Cleanup();
        }
    }
}
