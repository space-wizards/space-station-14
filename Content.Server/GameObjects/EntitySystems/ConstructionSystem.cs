#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Audio;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
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
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;


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
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private Dictionary<ICommonSession, HashSet<int>> _beingBuilt = new Dictionary<ICommonSession, HashSet<int>>();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<TryStartStructureConstructionMessage>(HandleStartStructureConstruction);
            SubscribeNetworkEvent<TryStartItemConstructionMessage>(HandleStartItemConstruction);
        }

        private async void HandleStartItemConstruction(TryStartItemConstructionMessage ev, EntitySessionEventArgs args)
        {
            var constructionPrototype = _prototypeManager.Index<ConstructionPrototype>(ev.PrototypeName);
            var constructionGraph = _prototypeManager.Index<ConstructionGraphPrototype>(constructionPrototype.Graph);
            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];
            var targetNode = constructionGraph.Nodes[constructionPrototype.TargetNode];
            var pathFind = constructionGraph.Path(startNode.Name, targetNode.Name);

            var user = args.SenderSession.AttachedEntity;

            if (user == null || !ActionBlockerSystem.CanInteract(user)) return;

            if (!user.TryGetComponent(out HandsComponent? hands)) return;

            if(pathFind == null)
                throw new InvalidDataException($"Can't find path from starting node to target node in construction! Recipe: {ev.PrototypeName}");

            var edge = startNode.GetEdge(pathFind[0].Name);
            var edgeTarget = pathFind[0];

            if(edge == null)
                throw new InvalidDataException($"Can't find edge from starting node to the next node in pathfinding! Recipe: {ev.PrototypeName}");

            // No support for conditions here!

            foreach (var step in edge.Steps)
            {
                switch (step)
                {
                    case ToolConstructionGraphStep _:
                    case NestedConstructionGraphStep _:
                        throw new InvalidDataException("Invalid first step for item recipe!");
                }
            }

            // We need a place to hold our construction items!
            var container = ContainerManagerComponent.Ensure<Container>("item_construction", user, out bool existed);

            if (existed)
            {
                user.PopupMessageCursor(Loc.GetString("You can't start another construction now!"));
                return;
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
                    var c = ContainerManagerComponent.Ensure<Container>(random.ToString(), user!, out var existed);

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

            // Maybe make this have a limit to prevent lagging?
            IEnumerable<IEntity> Enumerate()
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

                foreach (var near in _entityManager.GetEntitiesInRange(user!, 2f, true))
                {
                    yield return near;
                }
            }

            var failed = false;

            foreach (var step in edge.Steps)
            {
                doAfterTime += step.DoAfter;

                var handled = false;

                switch (step)
                {
                    case MaterialConstructionGraphStep materialStep:
                        foreach (var entity in Enumerate())
                        {
                            if (!entity.TryGetComponent(out StackComponent? stack) || stack.StackType.Equals(materialStep.Material))
                                continue;

                            if (!stack.Split(materialStep.Amount, user.ToCoordinates(), out var newStack))
                                continue;

                            if (string.IsNullOrEmpty(materialStep.Store))
                            {
                                if (!container.Insert(newStack))
                                    continue;
                            }
                            else if (!GetContainer(materialStep.Store).Insert(newStack))
                                    continue;

                            handled = true;
                            break;
                        }

                        break;

                    case ComponentConstructionGraphStep componentStep:
                        foreach (var entity in Enumerate())
                        {
                            if (!entity.HasComponent(_componentFactory.GetRegistration(componentStep.Component).Type))
                                continue;

                            if (string.IsNullOrEmpty(componentStep.Store))
                            {
                                if (!container.Insert(entity))
                                    continue;
                            }
                            else if (!GetContainer(componentStep.Store).Insert(entity))
                                continue;

                            handled = true;
                            break;
                        }

                        break;

                    case PrototypeConstructionGraphStep prototypeStep:
                        foreach (var entity in Enumerate())
                        {
                            if (entity.Prototype?.ID != prototypeStep.Prototype)
                                continue;

                            if (string.IsNullOrEmpty(prototypeStep.Store))
                            {
                                if (!container.Insert(entity))
                                    continue;
                            }
                            else if (!GetContainer(prototypeStep.Store).Insert(entity))
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
            }

            if (failed)
            {
                user.PopupMessageCursor(Loc.GetString("You don't have the materials to build that!"));
                FailCleanup();
                return;
            }

            var doAfterSystem = Get<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(user, doAfterTime)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = false,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            if (await doAfterSystem.DoAfter(doAfterArgs) == DoAfterStatus.Cancelled)
            {
                FailCleanup();
                return;
            }

            var item = _entityManager.SpawnEntity(edgeTarget.Entity, user.Transform.Coordinates);

            // Yes, this should throw if it's missing the component.
            var construction = item.GetComponent<ConstructionComponent>();

            // We attempt to set the pathfinding target.
            construction.Target = targetNode;

            // We preserve the containers...
            foreach (var (name, cont) in containers)
            {
                var newCont = ContainerManagerComponent.Ensure<Container>(name, item);

                foreach (var entity in cont.ContainedEntities.ToArray())
                {
                    cont.ForceRemove(entity);
                    newCont.Insert(entity);
                }
            }

            // We now get rid of all them.
            ShutdownContainers();

            // We do have completed effects!
            foreach (var completed in edge.Completed)
            {
                await completed.Completed(item);
            }

            if(item.TryGetComponent(out ItemComponent? itemComp))
                hands.PutInHandOrDrop(itemComp);
        }

        private async void HandleStartStructureConstruction(TryStartStructureConstructionMessage ev, EntitySessionEventArgs args)
        {
            var constructionPrototype = _prototypeManager.Index<ConstructionPrototype>(ev.PrototypeName);
            var constructionGraph = _prototypeManager.Index<ConstructionGraphPrototype>(constructionPrototype.Graph);
            var startNode = constructionGraph.Nodes[constructionPrototype.StartNode];

            var user = args.SenderSession.AttachedEntity;

            if (_beingBuilt.TryGetValue(args.SenderSession, out var set))
            {
                if (set.Contains(ev.Ack))
                {
                    user.PopupMessageCursor(Loc.GetString("You are already building that!"));
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
                    return;
            }

            void Cleanup()
            {
                _beingBuilt[args.SenderSession].Remove(ev.Ack);
            }

            if (user == null
                || !ActionBlockerSystem.CanInteract(user)
                || !user.TryGetComponent(out HandsComponent? hands) || hands.GetActiveHand == null
                || !user.InRangeUnobstructed(ev.Location, ignoreInsideBlocker:constructionPrototype.CanBuildInImpassable))
            {
                Cleanup();
                return;
            }

            var holding = hands.GetActiveHand.Owner;

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

                        // We have step completions!
                        foreach (var completed in firstStep.Completed)
                        {
                            await completed.StepCompleted(entity);

                            if (entity.Deleted)
                                return;
                        }

                        // Play the sound!
                        var sound = firstStep.GetSound();
                        if(!string.IsNullOrEmpty(sound))
                            Get<AudioSystem>().PlayFromEntity(sound, entity, AudioHelpers.WithVariation(0.125f));

                        // Yes, this should throw if it's missing the component.
                        var construction = entity.GetComponent<ConstructionComponent>();

                        // We attempt to set the pathfinding target.
                        if (!string.IsNullOrEmpty(constructionPrototype.TargetNode))
                            construction.Target = constructionGraph.Nodes[constructionPrototype.TargetNode];

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

                        Cleanup();
                        return;
                }
            }

            Cleanup();
        }
    }
}

