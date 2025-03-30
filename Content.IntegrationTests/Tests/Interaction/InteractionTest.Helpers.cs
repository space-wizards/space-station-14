#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Content.Client.Construction;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Components;
using Content.Server.Gravity;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Gravity;
using Content.Shared.Item;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using ItemToggleComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleComponent;

namespace Content.IntegrationTests.Tests.Interaction;

// This partial class defines various methods that are useful for performing & validating interactions
public abstract partial class InteractionTest
{
    /// <summary>
    /// Begin constructing an entity.
    /// </summary>
    protected async Task StartConstruction(string prototype, bool shouldSucceed = true)
    {
        var proto = ProtoMan.Index<ConstructionPrototype>(prototype);
        Assert.That(proto.Type, Is.EqualTo(ConstructionType.Structure));

        await Client.WaitPost(() =>
        {
            Assert.That(CConSys.TrySpawnGhost(proto, CEntMan.GetCoordinates(TargetCoords), Direction.South, out var clientTarget),
                Is.EqualTo(shouldSucceed));

            if (!shouldSucceed)
                return;

            var comp = CEntMan.GetComponent<ConstructionGhostComponent>(clientTarget!.Value);
            Target = CEntMan.GetNetEntity(clientTarget.Value);
            Assert.That(Target.Value.IsClientSide());
            ConstructionGhostId = clientTarget.Value.GetHashCode();
        });

        await RunTicks(1);
    }

    /// <summary>
    /// Craft an item.
    /// </summary>
    protected async Task CraftItem(string prototype, bool shouldSucceed = true)
    {
        Assert.That(ProtoMan.Index<ConstructionPrototype>(prototype).Type, Is.EqualTo(ConstructionType.Item));

        // Please someone purge async construction code
        Task<bool> task = default!;
        await Server.WaitPost(() =>
        {
            task = SConstruction.TryStartItemConstruction(prototype, SEntMan.GetEntity(Player));
        });

        Task? tickTask = null;
        while (!task.IsCompleted)
        {
            tickTask = Pair.RunTicksSync(1);
            await Task.WhenAny(task, tickTask);
        }

        if (tickTask != null)
            await tickTask;

#pragma warning disable RA0004
        Assert.That(task.Result, Is.EqualTo(shouldSucceed));
#pragma warning restore RA0004

        await RunTicks(5);
    }

    /// <summary>
    /// Spawn an entity entity and set it as the target.
    /// </summary>
    [MemberNotNull(nameof(Target), nameof(STarget), nameof(CTarget))]
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    protected async Task<NetEntity> SpawnTarget(string prototype)
    {
        Target = NetEntity.Invalid;
        await Server.WaitPost(() =>
        {
            Target = SEntMan.GetNetEntity(SEntMan.SpawnAtPosition(prototype, SEntMan.GetCoordinates(TargetCoords)));
        });

        await RunTicks(5);
        AssertPrototype(prototype);
        return Target!.Value;
    }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.

    /// <summary>
    /// Spawn an entity in preparation for deconstruction
    /// </summary>
    protected async Task StartDeconstruction(string prototype)
    {
        await SpawnTarget(prototype);
        var serverTarget = SEntMan.GetEntity(Target);
        Assert.That(SEntMan.TryGetComponent(serverTarget, out ConstructionComponent? comp));
        await Server.WaitPost(() => SConstruction.SetPathfindingTarget(serverTarget!.Value, comp!.DeconstructionNode, comp));
        await RunTicks(5);
    }

    /// <summary>
    /// Drops and deletes the currently held entity.
    /// </summary>
    protected async Task DeleteHeldEntity()
    {
        if (Hands.ActiveHandEntity is { } held)
        {
            await Server.WaitPost(() =>
            {
                Assert.That(HandSys.TryDrop(SEntMan.GetEntity(Player), null, false, true, Hands));
                SEntMan.DeleteEntity(held);
                SLogger.Debug($"Deleting held entity");
            });
        }

        await RunTicks(1);
        Assert.That(Hands.ActiveHandEntity, Is.Null);
    }

    /// <summary>
    /// Place an entity prototype into the players hand. Deletes any currently held entity.
    /// </summary>
    /// <param name="id">The entity or stack prototype to spawn and place into the users hand</param>
    /// <param name="quantity">The number of entities to spawn. If the prototype is a stack, this sets the stack count.</param>
    /// <param name="enableToggleable">Whether or not to automatically enable any toggleable items</param>
    protected async Task<NetEntity> PlaceInHands(string id, int quantity = 1, bool enableToggleable = true)
    {
        return await PlaceInHands((id, quantity), enableToggleable);
    }

    /// <summary>
    /// Place an entity prototype into the players hand. Deletes any currently held entity.
    /// </summary>
    /// <param name="entity">The entity type & quantity to spawn and place into the users hand</param>
    /// <param name="enableToggleable">Whether or not to automatically enable any toggleable items</param>
    protected async Task<NetEntity> PlaceInHands(EntitySpecifier entity, bool enableToggleable = true)
    {
        if (Hands.ActiveHand == null)
        {
            Assert.Fail("No active hand");
            return default;
        }

        Assert.That(!string.IsNullOrWhiteSpace(entity.Prototype));
        await DeleteHeldEntity();

        // spawn and pick up the new item
        var item = await SpawnEntity(entity, SEntMan.GetCoordinates(PlayerCoords));
        ItemToggleComponent? itemToggle = null;

        await Server.WaitPost(() =>
        {
            var playerEnt = SEntMan.GetEntity(Player);

            Assert.That(HandSys.TryPickup(playerEnt, item, Hands.ActiveHand, false, false, Hands));

            // turn on welders
            if (enableToggleable && SEntMan.TryGetComponent(item, out itemToggle) && !itemToggle.Activated)
            {
                Assert.That(ItemToggleSys.TryActivate((item, itemToggle), user: playerEnt));
            }
        });

        await RunTicks(1);
        Assert.That(Hands.ActiveHandEntity, Is.EqualTo(item));
        if (enableToggleable && itemToggle != null)
            Assert.That(itemToggle.Activated);

        return SEntMan.GetNetEntity(item);
    }

    /// <summary>
    /// Pick up an entity. Defaults to just deleting the previously held entity.
    /// </summary>
    protected async Task Pickup(NetEntity? entity = null, bool deleteHeld = true)
    {
        entity ??= Target;

        if (Hands.ActiveHand == null)
        {
            Assert.Fail("No active hand");
            return;
        }

        if (deleteHeld)
            await DeleteHeldEntity();

        var uid = SEntMan.GetEntity(entity);

        if (!SEntMan.TryGetComponent(uid, out ItemComponent? item))
        {
            Assert.Fail($"Entity {entity} is not an item");
            return;
        }

        await Server.WaitPost(() =>
        {
            Assert.That(HandSys.TryPickup(SEntMan.GetEntity(Player), uid.Value, Hands.ActiveHand, false, false, Hands, item));
        });

        await RunTicks(1);
        Assert.That(Hands.ActiveHandEntity, Is.EqualTo(uid));
    }

    /// <summary>
    /// Drops the currently held entity.
    /// </summary>
    protected async Task Drop()
    {
        if (Hands.ActiveHandEntity == null)
        {
            Assert.Fail("Not holding any entity to drop");
            return;
        }

        await Server.WaitPost(() =>
        {
            Assert.That(HandSys.TryDrop(SEntMan.GetEntity(Player), handsComp: Hands));
        });

        await RunTicks(1);
        Assert.That(Hands.ActiveHandEntity, Is.Null);
    }

    #region Interact

    /// <summary>
    /// Use the currently held entity.
    /// </summary>
    protected async Task UseInHand()
    {
        if (Hands.ActiveHandEntity is not { } target)
        {
            Assert.Fail("Not holding any entity");
            return;
        }

        await Server.WaitPost(() =>
        {
            InteractSys.UserInteraction(SEntMan.GetEntity(Player), SEntMan.GetComponent<TransformComponent>(target).Coordinates, target);
        });
    }

    /// <summary>
    /// Place an entity prototype into the players hand and interact with the given entity (or target position)
    /// </summary>
    /// <param name="id">The entity or stack prototype to spawn and place into the users hand</param>
    /// <param name="quantity">The number of entities to spawn. If the prototype is a stack, this sets the stack count.</param>
    /// <param name="awaitDoAfters">Whether or not to wait for any do-afters to complete</param>
    protected async Task InteractUsing(string id, int quantity = 1, bool awaitDoAfters = true)
    {
        await InteractUsing((id, quantity), awaitDoAfters);
    }

    /// <summary>
    /// Place an entity prototype into the players hand and interact with the given entity (or target position).
    /// </summary>
    /// <param name="entity">The entity type & quantity to spawn and place into the users hand</param>
    /// <param name="awaitDoAfters">Whether or not to wait for any do-afters to complete</param>
    protected async Task InteractUsing(EntitySpecifier entity, bool awaitDoAfters = true)
    {
        // For every interaction, we will also examine the entity, just in case this breaks something, somehow.
        // (e.g., servers attempt to assemble construction examine hints).
        if (Target != null)
        {
            await Client.WaitPost(() => ExamineSys.DoExamine(CEntMan.GetEntity(Target.Value)));
        }

        await PlaceInHands(entity);
        await Interact(awaitDoAfters);
    }

    /// <summary>
    /// Interact with an entity using the currently held entity.
    /// </summary>
    /// <param name="awaitDoAfters">Whether or not to wait for any do-afters to complete</param>
    protected async Task Interact(bool awaitDoAfters = true)
    {
        if (Target == null || !Target.Value.IsClientSide())
        {
            await Interact(Target, TargetCoords, awaitDoAfters);
            return;
        }

        // The target is a client-side entity, so we will just attempt to start construction under the assumption that
        // it is a construction ghost.

        await Client.WaitPost(() => CConSys.TryStartConstruction(CTarget!.Value));
        await RunTicks(5);

        if (awaitDoAfters)
            await AwaitDoAfters();

        await CheckTargetChange();
    }

    /// <inheritdoc cref="Interact(EntityUid?,EntityCoordinates,bool)"/>
    protected async Task Interact(NetEntity? target, NetCoordinates coordinates, bool awaitDoAfters = true)
    {
        Assert.That(SEntMan.TryGetEntity(target, out var sTarget) || target == null);
        var coords = SEntMan.GetCoordinates(coordinates);
        Assert.That(coords.IsValid(SEntMan));
        await Interact(sTarget, coords, awaitDoAfters);
    }

    /// <summary>
    /// Interact with an entity using the currently held entity.
    /// </summary>
    protected async Task Interact(EntityUid? target, EntityCoordinates coordinates, bool awaitDoAfters = true)
    {
        Assert.That(SEntMan.TryGetEntity(Player, out var player));

        await Server.WaitPost(() => InteractSys.UserInteraction(player!.Value, coordinates, target));
        await RunTicks(1);

        if (awaitDoAfters)
            await AwaitDoAfters();

        await CheckTargetChange();
    }

    /// <summary>
    /// Activate an entity.
    /// </summary>
    protected async Task Activate(NetEntity? target = null, bool awaitDoAfters = true)
    {
        target ??= Target;
        Assert.That(target, Is.Not.Null);
        Assert.That(SEntMan.TryGetEntity(target!.Value, out var sTarget));
        Assert.That(SEntMan.TryGetEntity(Player, out var player));

        await Server.WaitPost(() => InteractSys.InteractionActivate(player!.Value, sTarget!.Value));
        await RunTicks(1);

        if (awaitDoAfters)
            await AwaitDoAfters();

        await CheckTargetChange();
    }

    /// <summary>
    /// Variant of <see cref="InteractUsing(string,int,bool)"/> that performs several interactions using different entities.
    /// Useful for quickly finishing multiple construction steps.
    /// </summary>
    /// <remarks>
    /// Empty strings imply empty hands.
    /// </remarks>
    protected async Task Interact(params EntitySpecifier[] specifiers)
    {
        foreach (var spec in specifiers)
        {
            await InteractUsing(spec);
        }
    }

    /// <summary>
    /// Throw the currently held entity. Defaults to targeting the current <see cref="TargetCoords"/>
    /// </summary>
    protected async Task<bool> ThrowItem(NetCoordinates? target = null, float minDistance = 4)
    {
        var actualTarget = SEntMan.GetCoordinates(target ?? TargetCoords);
        var result = false;
        await Server.WaitPost(() => result = HandSys.ThrowHeldItem(SEntMan.GetEntity(Player), actualTarget, minDistance));
        return result;
    }

    #endregion

    /// <summary>
    /// Wait for any currently active DoAfters to finish.
    /// </summary>
    protected async Task AwaitDoAfters(int maxExpected = 1)
    {
        if (!ActiveDoAfters.Any())
            return;

        // Generally expect interactions to only start one DoAfter.
        Assert.That(ActiveDoAfters.Count(), Is.LessThanOrEqualTo(maxExpected));

        // wait out the DoAfters.
        var doAfters = ActiveDoAfters.ToList();
        while (ActiveDoAfters.Any())
        {
            await RunTicks(10);
        }

        foreach (var doAfter in doAfters)
        {
            Assert.That(!doAfter.Cancelled);
        }

        await RunTicks(5);
    }

    /// <summary>
    /// Cancel any currently active DoAfters. Default arguments are such that it also checks that there is at least one
    /// active DoAfter to cancel.
    /// </summary>
    protected async Task CancelDoAfters(int minExpected = 1, int maxExpected = 1)
    {
        Assert.That(ActiveDoAfters.Count(), Is.GreaterThanOrEqualTo(minExpected));
        Assert.That(ActiveDoAfters.Count(), Is.LessThanOrEqualTo(maxExpected));

        if (!ActiveDoAfters.Any())
            return;

        // Cancel all the do-afters
        var doAfters = ActiveDoAfters.ToList();
        await Server.WaitPost(() =>
        {
            foreach (var doAfter in doAfters)
            {
                DoAfterSys.Cancel(SEntMan.GetEntity(Player), doAfter.Index, DoAfters);
            }
        });

        await RunTicks(1);

        foreach (var doAfter in doAfters)
        {
            Assert.That(doAfter.Cancelled);
        }

        Assert.That(ActiveDoAfters.Count(), Is.EqualTo(0));
    }

    /// <summary>
    /// Check if the test's target entity has changed. E.g., construction interactions will swap out entities while
    /// a structure is being built.
    /// </summary>
    protected async Task CheckTargetChange()
    {
        if (Target == null)
            return;

        var originalTarget = Target.Value;
        await RunTicks(5);

        if (Target.Value.IsClientSide() && CTestSystem.Ghosts.TryGetValue(ConstructionGhostId, out var newWeh))
        {
            CLogger.Debug($"Construction ghost {ConstructionGhostId} became entity {newWeh}");
            Target = newWeh;
        }

        if (STestSystem.EntChanges.TryGetValue(Target.Value, out var newServerWeh))
        {
            SLogger.Debug($"Construction entity {Target.Value} changed to {newServerWeh}");
            Target = newServerWeh;
        }

        if (Target != originalTarget)
            await CheckTargetChange();
    }

    #region Asserts

    protected void ClientAssertPrototype(string? prototype, NetEntity? target = null)
    {
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return;
        }

        var meta = CEntMan.GetComponent<MetaDataComponent>(CEntMan.GetEntity(target.Value));
        Assert.That(meta.EntityPrototype?.ID, Is.EqualTo(prototype));
    }

    protected void AssertPrototype(string? prototype, NetEntity? target = null)
    {
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return;
        }

        var meta = SEntMan.GetComponent<MetaDataComponent>(SEntMan.GetEntity(target.Value));
        Assert.That(meta.EntityPrototype?.ID, Is.EqualTo(prototype));
    }

    protected void AssertAnchored(bool anchored = true, NetEntity? target = null)
    {
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return;
        }

        var sXform = SEntMan.GetComponent<TransformComponent>(SEntMan.GetEntity(target.Value));
        var cXform = CEntMan.GetComponent<TransformComponent>(CEntMan.GetEntity(target.Value));

        Assert.Multiple(() =>
        {
            Assert.That(sXform.Anchored, Is.EqualTo(anchored));
            Assert.That(cXform.Anchored, Is.EqualTo(anchored));
        });
    }

    protected void AssertDeleted(NetEntity? target = null)
    {
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(SEntMan.Deleted(SEntMan.GetEntity(target)));
            Assert.That(CEntMan.Deleted(CEntMan.GetEntity(target)));
        });
    }

    protected void AssertExists(NetEntity? target = null)
    {
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(SEntMan.EntityExists(SEntMan.GetEntity(target)));
            Assert.That(CEntMan.EntityExists(CEntMan.GetEntity(target)));
        });
    }

    /// <summary>
    /// Assert whether or not the target has the given component.
    /// </summary>
    protected void AssertComp<T>(bool hasComp = true, NetEntity? target = null) where T : IComponent
    {
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return;
        }

        Assert.That(SEntMan.HasComponent<T>(SEntMan.GetEntity(target)), Is.EqualTo(hasComp));
    }

    /// <summary>
    /// Check that the tile at the target position matches some prototype.
    /// </summary>
    protected async Task AssertTile(string? proto, NetCoordinates? coords = null)
    {
        var targetTile = proto == null
            ? Tile.Empty
            : new Tile(TileMan[proto].TileId);

        var tile = Tile.Empty;
        var serverCoords = SEntMan.GetCoordinates(coords ?? TargetCoords);
        var pos = Transform.ToMapCoordinates(serverCoords);
        await Server.WaitPost(() =>
        {
            if (MapMan.TryFindGridAt(pos, out var gridUid, out var grid))
                tile = MapSystem.GetTileRef(gridUid, grid, serverCoords).Tile;
        });

        Assert.That(tile.TypeId, Is.EqualTo(targetTile.TypeId));
    }

    protected void AssertGridCount(int value)
    {
        var count = 0;
        var query = SEntMan.AllEntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (query.MoveNext(out _, out var xform))
        {
            if (xform.MapUid == MapData.MapUid)
                count++;
        }

        Assert.That(count, Is.EqualTo(value));
    }

    #endregion

    #region Entity lookups

    /// <summary>
    /// Returns entities in an area around the target. Ignores the map, grid, player, target, and contained entities.
    /// </summary>
    protected async Task<HashSet<EntityUid>> DoEntityLookup(LookupFlags flags = LookupFlags.Uncontained)
    {
        var lookup = SEntMan.System<EntityLookupSystem>();

        HashSet<EntityUid> entities = default!;
        await Server.WaitPost(() =>
        {
            // Get all entities left behind by deconstruction
            entities = lookup.GetEntitiesIntersecting(MapId, Box2.CentredAroundZero(new Vector2(10, 10)), flags);

            var xformQuery = SEntMan.GetEntityQuery<TransformComponent>();

            HashSet<EntityUid> toRemove = new();
            foreach (var ent in entities)
            {
                var transform = xformQuery.GetComponent(ent);
                var netEnt = SEntMan.GetNetEntity(ent);

                if (ent == transform.MapUid
                    || ent == transform.GridUid
                    || netEnt == Player
                    || netEnt == Target)
                {
                    toRemove.Add(ent);
                }
            }

            entities.ExceptWith(toRemove);
        });

        return entities;
    }

    /// <summary>
    /// Performs an entity lookup and asserts that only the listed entities exist and that they are all present.
    /// Ignores the grid, map, player, target and contained entities.
    /// </summary>
    protected async Task AssertEntityLookup(params EntitySpecifier[] entities)
    {
        var collection = new EntitySpecifierCollection(entities);
        await AssertEntityLookup(collection);
    }

    /// <summary>
    /// Performs an entity lookup and asserts that only the listed entities exist and that they are all present.
    /// Ignores the grid, map, player, target, contained entities, and entities with null prototypes.
    /// </summary>
    protected async Task AssertEntityLookup(
        EntitySpecifierCollection collection,
        bool failOnMissing = true,
        bool failOnExcess = true,
        LookupFlags flags = LookupFlags.Uncontained)
    {
        var expected = collection.Clone();
        var entities = await DoEntityLookup(flags);
        var found = ToEntityCollection(entities);
        expected.Remove(found);
        await expected.ConvertToStacks(ProtoMan, Factory, Server);

        if (expected.Entities.Count == 0)
            return;

        Assert.Multiple(() =>
        {
            foreach (var (proto, quantity) in expected.Entities)
            {
                if (proto == "Audio")
                    continue;

                if (quantity < 0 && failOnExcess)
                    Assert.Fail($"Unexpected entity/stack: {proto}, quantity: {-quantity}");

                if (quantity > 0 && failOnMissing)
                    Assert.Fail($"Missing entity/stack: {proto}, quantity: {quantity}");

                if (quantity == 0)
                    throw new Exception("Error in entity collection math.");
            }
        });
    }

    /// <summary>
    /// Performs an entity lookup and attempts to find an entity matching the given entity specifier.
    /// </summary>
    /// <remarks>
    /// This is used to check that an item-crafting attempt was successful. Ideally crafting items would just return the
    /// entity or raise an event or something.
    /// </remarks>
    protected async Task<EntityUid> FindEntity(
        EntitySpecifier spec,
        LookupFlags flags = LookupFlags.Uncontained | LookupFlags.Contained,
        bool shouldSucceed = true)
    {
        await spec.ConvertToStack(ProtoMan, Factory, Server);

        var entities = await DoEntityLookup(flags);
        foreach (var uid in entities)
        {
            var found = ToEntitySpecifier(uid);
            if (found is null)
                continue;

            if (spec.Prototype != found.Prototype)
                continue;

            if (found.Quantity >= spec.Quantity)
                return uid;

            // TODO combine stacks?
        }

        if (shouldSucceed)
            Assert.Fail($"Could not find stack/entity with prototype {spec.Prototype}");

        return default;
    }

    #endregion

    /// <summary>
    /// List of currently active DoAfters on the player.
    /// </summary>
    protected IEnumerable<Shared.DoAfter.DoAfter> ActiveDoAfters
        => DoAfters.DoAfters.Values.Where(x => !x.Cancelled && !x.Completed);

    #region Component

    /// <summary>
    /// Convenience method to get components on the target. Returns SERVER-SIDE components.
    /// </summary>
    protected T Comp<T>(NetEntity? target = null) where T : IComponent
    {
        target ??= Target;
        if (target == null)
            Assert.Fail("No target specified");

        return SEntMan.GetComponent<T>(ToServer(target!.Value));
    }

    /// <inheritdoc cref="Comp{T}"/>
    protected bool TryComp<T>(NetEntity? target, [NotNullWhen(true)] out T? comp) where T : IComponent
    {
        return SEntMan.TryGetComponent(ToServer(target), out comp);
    }

    /// <inheritdoc cref="Comp{T}"/>
    protected bool TryComp<T>([NotNullWhen(true)] out T? comp) where T : IComponent
    {
        return SEntMan.TryGetComponent(STarget, out comp);
    }

    #endregion

    /// <summary>
    /// Set the tile at the target position to some prototype.
    /// </summary>
    protected async Task SetTile(string? proto, NetCoordinates? coords = null, Entity<MapGridComponent>? grid = null)
    {
        var tile = proto == null
            ? Tile.Empty
            : new Tile(TileMan[proto].TileId);

        var pos = Transform.ToMapCoordinates(SEntMan.GetCoordinates(coords ?? TargetCoords));

        EntityUid gridUid;
        MapGridComponent? gridComp;
        await Server.WaitPost(() =>
        {
            if (grid is { } gridEnt)
            {
                MapSystem.SetTile(gridEnt, SEntMan.GetCoordinates(coords ?? TargetCoords), tile);
                return;
            }
            else if (MapMan.TryFindGridAt(pos, out var gUid, out var gComp))
            {
                MapSystem.SetTile(gUid, gComp, SEntMan.GetCoordinates(coords ?? TargetCoords), tile);
                return;
            }

            if (proto == null)
                return;

            gridEnt = MapMan.CreateGridEntity(MapData.MapId);
            grid = gridEnt;
            gridUid = gridEnt;
            gridComp = gridEnt.Comp;
            var gridXform = SEntMan.GetComponent<TransformComponent>(gridUid);
            Transform.SetWorldPosition(gridXform, pos.Position);
            MapSystem.SetTile((gridUid, gridComp), SEntMan.GetCoordinates(coords ?? TargetCoords), tile);

            if (!MapMan.TryFindGridAt(pos, out _, out _))
                Assert.Fail("Failed to create grid?");
        });
        await AssertTile(proto, coords);
    }

    protected async Task Delete(EntityUid uid)
    {
        await Server.WaitPost(() => SEntMan.DeleteEntity(uid));
        await RunTicks(5);
    }

    protected Task Delete(NetEntity nuid)
    {
        return Delete(SEntMan.GetEntity(nuid));
    }

    #region Time/Tick managment

    protected async Task RunTicks(int ticks)
    {
        await Pair.RunTicksSync(ticks);
    }

    protected async Task RunSeconds(float seconds)
    {
        await Pair.RunSeconds(seconds);
    }

    #endregion

    #region BUI
    /// <summary>
    ///     Sends a bui message using the given bui key.
    /// </summary>
    protected async Task SendBui(Enum key, BoundUserInterfaceMessage msg, EntityUid? _ = null)
    {
        if (!TryGetBui(key, out var bui))
            return;

        await Client.WaitPost(() => bui.SendMessage(msg));

        // allow for client -> server and server -> client messages to be sent.
        await RunTicks(15);
    }

    /// <summary>
    ///     Sends a bui message using the given bui key.
    /// </summary>
    protected async Task CloseBui(Enum key, EntityUid? _ = null)
    {
        if (!TryGetBui(key, out var bui))
            return;

        await Client.WaitPost(() => bui.Close());

        // allow for client -> server and server -> client messages to be sent.
        await RunTicks(15);
    }

    protected bool TryGetBui(Enum key, [NotNullWhen(true)] out BoundUserInterface? bui, NetEntity? target = null, bool shouldSucceed = true)
    {
        bui = null;
        target ??= Target;
        if (target == null)
        {
            Assert.Fail("No target specified");
            return false;
        }

        if (!CEntMan.TryGetComponent<UserInterfaceComponent>(CEntMan.GetEntity(target), out var ui))
        {
            if (shouldSucceed)
                Assert.Fail($"Entity {SEntMan.ToPrettyString(SEntMan.GetEntity(target.Value))} does not have a bui component");
            return false;
        }

        if (!ui.ClientOpenInterfaces.TryGetValue(key, out bui))
        {
            if (shouldSucceed)
                Assert.Fail($"Entity {SEntMan.ToPrettyString(SEntMan.GetEntity(target.Value))} does not have an open bui with key {key.GetType()}.{key}.");
            return false;
        }

        var bui2 = bui;
        Assert.Multiple(() =>
        {
            Assert.That(bui2.UiKey, Is.EqualTo(key), $"Bound user interface {bui2} is indexed by a key other than the one assigned to it somehow. {bui2.UiKey} != {key}");
            Assert.That(shouldSucceed, Is.True);
        });
        return true;
    }

    protected bool IsUiOpen(Enum key)
    {
        if (!TryComp(Player, out UserInterfaceUserComponent? user))
            return false;

        foreach (var keys in user.OpenInterfaces.Values)
        {
            if (keys.Contains(key))
                return true;
        }

        return false;
    }

    #endregion

    #region UI

    /// <summary>
    /// Attempts to find, and then presses and releases a control on some client-side window.
    /// Will fail if the control cannot be found.
    /// </summary>
    protected async Task ClickControl<TWindow, TControl>(string name, BoundKeyFunction? function = null)
        where TWindow : BaseWindow
        where TControl : Control
    {
        var window = GetWindow<TWindow>();
        var control = GetControlFromField<TControl>(name, window);
        await ClickControl(control, function);
    }

    /// <summary>
    /// Attempts to find, and then presses and releases a control on some client-side widget.
    /// Will fail if the control cannot be found.
    /// </summary>
    protected async Task ClickWidgetControl<TWidget, TControl>(string name, BoundKeyFunction? function = null)
        where TWidget : UIWidget, new()
        where TControl : Control
    {
        var widget = GetWidget<TWidget>();
        var control = GetControlFromField<TControl>(name, widget);
        await ClickControl(control, function);
    }

    /// <inheritdoc cref="ClickControl{TWindow,TControl}"/>
    protected async Task ClickControl<TWindow>(string name, BoundKeyFunction? function = null)
        where TWindow : BaseWindow
    {
        await ClickControl<TWindow, Control>(name, function);
    }

    /// <inheritdoc cref="ClickWidgetControl{TWidget,TControl}"/>
    protected async Task ClickWidgetControl<TWidget>(string name, BoundKeyFunction? function = null)
        where TWidget : UIWidget, new()
    {
        await ClickWidgetControl<TWidget, Control>(name, function);
    }

    /// <summary>
    ///     Simulates a click and release at the center of some UI control.
    /// </summary>
    protected async Task ClickControl(Control control, BoundKeyFunction? function = null)
    {
        function ??= EngineKeyFunctions.UIClick;
        var screenCoords = new ScreenCoordinates(
            control.GlobalPixelPosition + control.PixelSize / 2,
            control.Window?.Id ?? default);

        var relativePos = screenCoords.Position / control.UIScale - control.GlobalPosition;
        var relativePixelPos = screenCoords.Position - control.GlobalPixelPosition;

        var args = new GUIBoundKeyEventArgs(
            function.Value,
            BoundKeyState.Down,
            screenCoords,
            default,
            relativePos,
            relativePixelPos);

        await Client.DoGuiEvent(control, args);
        await RunTicks(1);

        args = new GUIBoundKeyEventArgs(
            function.Value,
            BoundKeyState.Up,
            screenCoords,
            default,
            relativePos,
            relativePixelPos);

        await Client.DoGuiEvent(control, args);
        await RunTicks(1);
    }

    /// <summary>
    /// Attempt to retrieve a control by looking for a field on some other control.
    /// </summary>
    /// <remarks>
    /// Will fail if the control cannot be found.
    /// </remarks>
    protected TControl GetControlFromField<TControl>(string name, Control parent)
        where TControl : Control
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var parentType = parent.GetType();
        var field = parentType.GetField(name, flags);
        var prop = parentType.GetProperty(name, flags);

        if (field == null && prop == null)
        {
            Assert.Fail($"Window {parentType.Name} does not have a field or property named {name}");
            return default!;
        }

        var fieldOrProp = field?.GetValue(parent) ?? prop?.GetValue(parent);

        if (fieldOrProp is not Control control)
        {
            Assert.Fail($"{name} was null or was not a control.");
            return default!;
        }

        Assert.That(control.GetType().IsAssignableTo(typeof(TControl)));
        return (TControl) control;
    }

    /// <summary>
    /// Attempt to retrieve a control that matches some predicate by iterating through a control's children.
    /// </summary>
    /// <remarks>
    /// Will fail if the control cannot be found.
    /// </remarks>
    protected TControl GetControlFromChildren<TControl>(Func<TControl, bool> predicate, Control parent, bool recursive = true)
        where TControl : Control
    {
        if (TryGetControlFromChildren(predicate, parent, out var control, recursive))
            return control;

        Assert.Fail($"Failed to find a {nameof(TControl)} that satisfies the predicate in {parent.Name}");
        return default!;
    }

    /// <summary>
    /// Attempt to retrieve a control of a given type by iterating through a control's children.
    /// </summary>
    protected TControl GetControlFromChildren<TControl>(Control parent, bool recursive = false)
        where TControl : Control
    {
        return GetControlFromChildren<TControl>(static _ => true, parent, recursive);
    }

    /// <summary>
    /// Attempt to retrieve a control that matches some predicate by iterating through a control's children.
    /// </summary>
    protected bool TryGetControlFromChildren<TControl>(
        Func<TControl, bool> predicate,
        Control parent,
        [NotNullWhen(true)] out TControl? control,
        bool recursive = true)
        where TControl : Control
    {
        foreach (var ctrl in parent.Children)
        {
            if (ctrl is TControl cast && predicate(cast))
            {
                control = cast;
                return true;
            }

            if (recursive && TryGetControlFromChildren(predicate, ctrl, out control))
                return true;
        }

        control = null;
        return false;
    }

    /// <summary>
    /// Attempts to find a currently open client-side window. Will fail if the window cannot be found.
    /// </summary>
    /// <remarks>
    /// Note that this just returns the very first open window of this type that is found.
    /// </remarks>
    protected TWindow GetWindow<TWindow>() where TWindow : BaseWindow
    {
        if (TryFindWindow(out TWindow? window))
            return window;

        Assert.Fail($"Could not find a window assignable to {nameof(TWindow)}");
        return default!;
    }

    /// <summary>
    /// Attempts to find a currently open client-side window.
    /// </summary>
    /// <remarks>
    /// Note that this just returns the very first open window of this type that is found.
    /// </remarks>
    protected bool TryFindWindow<TWindow>([NotNullWhen(true)] out TWindow? window) where TWindow : BaseWindow
    {
        TryFindWindow(typeof(TWindow), out var control);
        window = control as TWindow;
        return window != null;
    }

    /// <summary>
    /// Attempts to find a currently open client-side window.
    /// </summary>
    /// <remarks>
    /// Note that this just returns the very first open window of this type that is found.
    /// </remarks>
    protected bool TryFindWindow(Type type, [NotNullWhen(true)] out BaseWindow? window)
    {
        Assert.That(type.IsAssignableTo(typeof(BaseWindow)));
        window = UiMan.WindowRoot.Children
            .OfType<BaseWindow>()
            .Where(x => x.IsOpen)
            .FirstOrDefault(x => x.GetType().IsAssignableTo(type));

        return window != null;
    }


    /// <summary>
    /// Attempts to find client-side UI widget.
    /// </summary>
    protected UIWidget GetWidget<TWidget>()
        where TWidget : UIWidget, new()
    {
        if (TryFindWidget(out TWidget? widget))
            return widget;

        Assert.Fail($"Could not find a {typeof(TWidget).Name} widget");
        return default!;
    }

    /// <summary>
    /// Attempts to find client-side UI widget.
    /// </summary>
    private bool TryFindWidget<TWidget>([NotNullWhen(true)] out TWidget? uiWidget)
        where TWidget : UIWidget, new()
    {
        uiWidget = null;
        var screen = UiMan.ActiveScreen;
        if (screen == null)
            return false;

        return screen.TryGetWidget(out uiWidget);
    }

    #endregion

    #region Power

    protected void ToggleNeedPower(NetEntity? target = null)
    {
        var comp = Comp<ApcPowerReceiverComponent>(target);
        comp.NeedsPower = !comp.NeedsPower;
    }

    #endregion

    #region Map Setup

    /// <summary>
    /// Adds gravity to a given entity. Defaults to the grid if no entity is specified.
    /// </summary>
    protected async Task AddGravity(EntityUid? uid = null)
    {
        var target = uid ?? MapData.Grid;
        await Server.WaitPost(() =>
        {
            var gravity = SEntMan.EnsureComponent<GravityComponent>(target);
            SEntMan.System<GravitySystem>().EnableGravity(target, gravity);
        });
    }

    /// <summary>
    /// Adds a default atmosphere to the test map.
    /// </summary>
    protected async Task AddAtmosphere(EntityUid? uid = null)
    {
        var target = uid ?? MapData.MapUid;
        await Server.WaitPost(() =>
        {
            var atmosSystem = SEntMan.System<AtmosphereSystem>();
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;
            atmosSystem.SetMapAtmosphere(target, false, new GasMixture(moles, Atmospherics.T20C));
        });
    }

    #endregion

    #region Inputs



    /// <summary>
    ///     Make the client press and then release a key. This assumes the key is currently released.
    ///     This will default to using the <see cref="Target"/> entity and <see cref="TargetCoords"/> coordinates.
    /// </summary>
    protected async Task PressKey(
        BoundKeyFunction key,
        int ticks = 1,
        NetCoordinates? coordinates = null,
        NetEntity? cursorEntity = null)
    {
        await SetKey(key, BoundKeyState.Down, coordinates, cursorEntity);
        await RunTicks(ticks);
        await SetKey(key, BoundKeyState.Up, coordinates, cursorEntity);
        await RunTicks(1);
    }

    /// <summary>
    ///     Make the client press or release a key.
    ///     This will default to using the <see cref="Target"/> entity and <see cref="TargetCoords"/> coordinates.
    /// </summary>
    protected async Task SetKey(
        BoundKeyFunction key,
        BoundKeyState state,
        NetCoordinates? coordinates = null,
        NetEntity? cursorEntity = null,
        ScreenCoordinates? screenCoordinates = null)
    {
        var coords = coordinates ?? TargetCoords;
        var target = cursorEntity ?? Target ?? default;
        var screen = screenCoordinates ?? default;

        var funcId = InputManager.NetworkBindMap.KeyFunctionID(key);
        var message = new ClientFullInputCmdMessage(CTiming.CurTick, CTiming.TickFraction, funcId)
        {
            State = state,
            Coordinates = CEntMan.GetCoordinates(coords),
            ScreenCoordinates = screen,
            Uid = CEntMan.GetEntity(target),
        };

        await Client.WaitPost(() => InputSystem.HandleInputCommand(ClientSession, key, message));
    }

    /// <summary>
    ///     Variant of <see cref="SetKey"/> for setting movement keys.
    /// </summary>
    protected async Task SetMovementKey(DirectionFlag dir, BoundKeyState state)
    {
        if ((dir & DirectionFlag.South) != 0)
            await SetKey(EngineKeyFunctions.MoveDown, state);

        if ((dir & DirectionFlag.East) != 0)
            await SetKey(EngineKeyFunctions.MoveRight, state);

        if ((dir & DirectionFlag.North) != 0)
            await SetKey(EngineKeyFunctions.MoveUp, state);

        if ((dir & DirectionFlag.West) != 0)
            await SetKey(EngineKeyFunctions.MoveLeft, state);
    }

    /// <summary>
    ///     Make the client hold the move key in some direction for some amount of time.
    /// </summary>
    protected async Task Move(DirectionFlag dir, float seconds)
    {
        await SetMovementKey(dir, BoundKeyState.Down);
        await RunSeconds(seconds);
        await SetMovementKey(dir, BoundKeyState.Up);
        await RunTicks(1);
    }

    #endregion

    #region Networking

    protected EntityUid ToServer(NetEntity nent) => SEntMan.GetEntity(nent);
    protected EntityUid ToClient(NetEntity nent) => CEntMan.GetEntity(nent);
    protected EntityUid? ToServer(NetEntity? nent) => SEntMan.GetEntity(nent);
    protected EntityUid? ToClient(NetEntity? nent) => CEntMan.GetEntity(nent);
    protected EntityUid ToServer(EntityUid cuid) => SEntMan.GetEntity(CEntMan.GetNetEntity(cuid));
    protected EntityUid ToClient(EntityUid cuid) => CEntMan.GetEntity(SEntMan.GetNetEntity(cuid));
    protected EntityUid? ToServer(EntityUid? cuid) => SEntMan.GetEntity(CEntMan.GetNetEntity(cuid));
    protected EntityUid? ToClient(EntityUid? cuid) => CEntMan.GetEntity(SEntMan.GetNetEntity(cuid));

    protected EntityCoordinates ToServer(NetCoordinates coords) => SEntMan.GetCoordinates(coords);
    protected EntityCoordinates ToClient(NetCoordinates coords) => CEntMan.GetCoordinates(coords);
    protected EntityCoordinates? ToServer(NetCoordinates? coords) => SEntMan.GetCoordinates(coords);
    protected EntityCoordinates? ToClient(NetCoordinates? coords) => CEntMan.GetCoordinates(coords);

    #endregion

    #region Metadata & Transforms

    protected MetaDataComponent Meta(NetEntity uid) => Meta(ToServer(uid));
    protected MetaDataComponent Meta(EntityUid uid) => SEntMan.GetComponent<MetaDataComponent>(uid);

    protected TransformComponent Xform(NetEntity uid) => Xform(ToServer(uid));
    protected TransformComponent Xform(EntityUid uid) => SEntMan.GetComponent<TransformComponent>(uid);

    protected EntityCoordinates Position(NetEntity uid) => Position(ToServer(uid));
    protected EntityCoordinates Position(EntityUid uid) => Xform(uid).Coordinates;

    #endregion
}
