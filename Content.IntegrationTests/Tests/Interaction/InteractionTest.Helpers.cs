#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Client.Construction;
using Content.Server.Construction.Components;
using Content.Server.Tools.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Item;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

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
            Assert.That(CConSys.TrySpawnGhost(proto, TargetCoords, Direction.South, out Target),
                Is.EqualTo(shouldSucceed));

            if (!shouldSucceed)
                return;
            var comp = CEntMan.GetComponent<ConstructionGhostComponent>(Target!.Value);
            ConstructionGhostId = comp.GhostId;
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
        Task<bool> task =default!;
        await Server.WaitPost(() => task = SConstruction.TryStartItemConstruction(prototype, Player));

        Task? tickTask = null;
        while (!task.IsCompleted)
        {
            tickTask = PoolManager.RunTicksSync(PairTracker.Pair, 1);
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
    protected async Task SpawnTarget(string prototype)
    {
        await Server.WaitPost(() =>
        {
            Target = SEntMan.SpawnEntity(prototype, TargetCoords);
        });

        await RunTicks(5);
        AssertPrototype(prototype);
    }

    /// <summary>
    /// Spawn an entity in preparation for deconstruction
    /// </summary>
    protected async Task StartDeconstruction(string prototype)
    {
        await SpawnTarget(prototype);
        Assert.That(SEntMan.TryGetComponent(Target, out ConstructionComponent? comp));
        await Server.WaitPost(() => SConstruction.SetPathfindingTarget(Target!.Value, comp!.DeconstructionNode, comp));
        await RunTicks(5);
    }

    /// <summary>
    /// Drops and deletes the currently held entity.
    /// </summary>
    protected async Task DeleteHeldEntity()
    {
        if (Hands.ActiveHandEntity is {} held)
        {
            await Server.WaitPost(() =>
            {
                Assert.That(HandSys.TryDrop(Player, null, false, true, Hands));
                SEntMan.DeleteEntity(held);
                Logger.Debug($"Deleting held entity");
            });
        }

        await RunTicks(1);
        Assert.That(Hands.ActiveHandEntity == null);
    }

    /// <summary>
    /// Place an entity prototype into the players hand. Deletes any currently held entity.
    /// </summary>
    /// <remarks>
    /// Automatically enables welders.
    /// </remarks>
    protected async Task<EntityUid?> PlaceInHands(string? id, int quantity = 1, bool enableWelder = true)
        => await PlaceInHands(id == null ? null : (id, quantity), enableWelder);

    /// <summary>
    /// Place an entity prototype into the players hand. Deletes any currently held entity.
    /// </summary>
    /// <remarks>
    /// Automatically enables welders.
    /// </remarks>
    protected async Task<EntityUid?> PlaceInHands(EntitySpecifier? entity, bool enableWelder = true)
    {
        if (Hands.ActiveHand == null)
        {
            Assert.Fail("No active hand");
            return default;
        }

        await DeleteHeldEntity();

        if (entity == null)
        {
            await RunTicks(1);
            Assert.That(Hands.ActiveHandEntity == null);
            return null;
        }

        // spawn and pick up the new item
        EntityUid item = await SpawnEntity(entity, PlayerCoords);
        WelderComponent? welder = null;

        await Server.WaitPost(() =>
        {
            Assert.That(HandSys.TryPickup(Player, item, Hands.ActiveHand, false, false, false, Hands));

            // turn on welders
            if (enableWelder && SEntMan.TryGetComponent(item, out welder) && !welder.Lit)
                Assert.That(ToolSys.TryTurnWelderOn(item, Player, welder));
        });

        await RunTicks(1);
        Assert.That(Hands.ActiveHandEntity, Is.EqualTo(item));
        if (enableWelder && welder != null)
            Assert.That(welder.Lit);

        return item;
    }

    /// <summary>
    /// Pick up an entity. Defaults to just deleting the previously held entity.
    /// </summary>
    protected async Task Pickup(EntityUid? uid = null, bool deleteHeld = true)
    {
        uid ??= Target;

        if (Hands.ActiveHand == null)
        {
            Assert.Fail("No active hand");
            return;
        }

        if (deleteHeld)
            await DeleteHeldEntity();

        if (!SEntMan.TryGetComponent(uid, out ItemComponent? item))
        {
            Assert.Fail($"Entity {uid} is not an item");
            return;
        }

        await Server.WaitPost(() =>
        {
            Assert.That(HandSys.TryPickup(Player, uid!.Value, Hands.ActiveHand, false, false, false, Hands, item));
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
            Assert.That(HandSys.TryDrop(Player, handsComp: Hands));
        });

        await RunTicks(1);
        Assert.IsNull(Hands.ActiveHandEntity);
    }

    /// <summary>
    /// Use the currently held entity.
    /// </summary>
    protected async Task UseInHand()
    {
        if (Hands.ActiveHandEntity is not {} target)
        {
            Assert.Fail("Not holding any entity");
            return;
        }

        await Server.WaitPost(() =>
        {
            InteractSys.UserInteraction(Player, SEntMan.GetComponent<TransformComponent>(target).Coordinates, target);
        });
    }

    /// <summary>
    /// Place an entity prototype into the players hand and interact with the given entity (or target position)
    /// </summary>
    protected async Task Interact(string? id, int quantity = 1, bool shouldSucceed = true, bool awaitDoAfters = true)
        => await Interact(id == null ? null : (id, quantity), shouldSucceed, awaitDoAfters);

    /// <summary>
    /// Place an entity prototype into the players hand and interact with the given entity (or target position)
    /// </summary>
    protected async Task Interact(EntitySpecifier? entity, bool shouldSucceed = true, bool awaitDoAfters = true)
    {
        // For every interaction, we will also examine the entity, just in case this breaks something, somehow.
        // (e.g., servers attempt to assemble construction examine hints).
        if (Target != null)
        {
            await Client.WaitPost(() => ExamineSys.DoExamine(Target.Value));
        }

        await PlaceInHands(entity);

        if (Target == null || !Target.Value.IsClientSide())
        {
            await Server.WaitPost(() => InteractSys.UserInteraction(Player, TargetCoords, Target));
            await RunTicks(1);
        }
        else
        {
            // The entity is client-side, so attempt to start construction
            var ghost = CEntMan.GetComponent<ConstructionGhostComponent>(Target.Value);
            await Client.WaitPost(() => CConSys.TryStartConstruction(ghost.GhostId));
            await RunTicks(5);
        }

        if (awaitDoAfters)
            await AwaitDoAfters(shouldSucceed);

        await CheckTargetChange(shouldSucceed && awaitDoAfters);
    }

    /// <summary>
    /// Wait for any currently active DoAfters to finish.
    /// </summary>
    protected async Task AwaitDoAfters(bool shouldSucceed = true, int maxExpected = 1)
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

        if (!shouldSucceed)
            return;

        foreach (var doAfter in doAfters)
        {
            Assert.That(!doAfter.Cancelled);
        }
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
                DoAfterSys.Cancel(Player, doAfter.Index, DoAfters);
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
    protected async Task CheckTargetChange(bool shouldSucceed)
    {
        EntityUid newTarget = default;
        if (Target == null)
            return;
        var target = Target.Value;

        await RunTicks(5);

        if (target.IsClientSide())
        {
            Assert.That(CEntMan.Deleted(target), Is.EqualTo(shouldSucceed),
                $"Construction ghost was {(shouldSucceed ? "not deleted" : "deleted")}.");

            if (shouldSucceed)
            {
                Assert.That(CTestSystem.Ghosts.TryGetValue(ConstructionGhostId, out newTarget),
                    $"Failed to get construction entity from ghost Id");

                await Client.WaitPost(() => Logger.Debug($"Construction ghost {ConstructionGhostId} became entity {newTarget}"));
                Target = newTarget;
            }
        }

        if (STestSystem.EntChanges.TryGetValue(Target.Value, out newTarget))
        {
            await Server.WaitPost(
                () => Logger.Debug($"Construction entity {Target.Value} changed to {newTarget}"));

            Target = newTarget;
        }

        if (Target != target)
            await CheckTargetChange(shouldSucceed);
    }

    /// <summary>
    /// Variant of <see cref="InteractUsing"/> that performs several interactions using different entities.
    /// </summary>
    protected async Task Interact(params EntitySpecifier?[] specifiers)
    {
        foreach (var spec in specifiers)
        {
            await Interact(spec);
        }
    }

    #region Asserts

    protected void AssertPrototype(string? prototype)
    {
        var meta = Comp<MetaDataComponent>();
        Assert.That(meta.EntityPrototype?.ID, Is.EqualTo(prototype));
    }

    protected void AssertAnchored(bool anchored = true)
    {
        var sXform = SEntMan.GetComponent<TransformComponent>(Target!.Value);
        var cXform = CEntMan.GetComponent<TransformComponent>(Target.Value);
        Assert.That(sXform.Anchored, Is.EqualTo(anchored));
        Assert.That(cXform.Anchored, Is.EqualTo(anchored));
    }

    protected void AssertDeleted(bool deleted = true)
    {
        Assert.That(SEntMan.Deleted(Target), Is.EqualTo(deleted));
        Assert.That(CEntMan.Deleted(Target), Is.EqualTo(deleted));
    }

    /// <summary>
    /// Assert whether or not the target has the given component.
    /// </summary>
    protected void AssertComp<T>(bool hasComp = true)
    {
        Assert.That(SEntMan.HasComponent<T>(Target), Is.EqualTo(hasComp));
    }

    /// <summary>
    /// Check that the tile at the target position matches some prototype.
    /// </summary>
    protected async Task AssertTile(string? proto, EntityCoordinates? coords = null)
    {
        var targetTile = proto == null
            ? Tile.Empty
            : new Tile(TileMan[proto].TileId);

        Tile tile = Tile.Empty;
        var pos = (coords ?? TargetCoords).ToMap(SEntMan, Transform);
        await Server.WaitPost(() =>
        {
            if (MapMan.TryFindGridAt(pos, out var grid))
                tile = grid.GetTileRef(coords ?? TargetCoords).Tile;
        });

        Assert.That(tile.TypeId, Is.EqualTo(targetTile.TypeId));
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
            entities = lookup.GetEntitiesIntersecting(MapId, Box2.CentredAroundZero((10, 10)), flags);

            var xformQuery = SEntMan.GetEntityQuery<TransformComponent>();

            HashSet<EntityUid> toRemove = new();
            foreach (var ent in entities)
            {
                var transform = xformQuery.GetComponent(ent);

                if (ent == transform.MapUid
                    || ent == transform.GridUid
                    || ent == Player
                    || ent == Target)
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
    /// Ignores the grid, map, player, target and contained entities.
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
        expected.ConvertToStacks(ProtoMan, Factory);

        if (expected.Entities.Count == 0)
            return;

        Assert.Multiple(() =>
        {
            foreach (var (proto, quantity) in expected.Entities)
            {
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
        spec.ConvertToStack(ProtoMan, Factory);

        var entities = await DoEntityLookup(flags);
        foreach (var uid in entities)
        {
            var found = ToEntitySpecifier(uid);
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

    /// <summary>
    /// Convenience method to get components on the target. Returns SERVER-SIDE components.
    /// </summary>
    protected T Comp<T>() => SEntMan.GetComponent<T>(Target!.Value);

    /// <summary>
    /// Set the tile at the target position to some prototype.
    /// </summary>
    protected async Task SetTile(string? proto, EntityCoordinates? coords = null, MapGridComponent? grid = null)
    {
        var tile = proto == null
            ? Tile.Empty
            : new Tile(TileMan[proto].TileId);

        var pos = (coords ?? TargetCoords).ToMap(SEntMan, Transform);

        await Server.WaitPost(() =>
        {
            if (grid != null || MapMan.TryFindGridAt(pos, out grid))
            {
                grid.SetTile(coords ?? TargetCoords, tile);
                return;
            }

            if (proto == null)
                return;

            grid = MapMan.CreateGrid(MapData.MapId);
            var gridXform = SEntMan.GetComponent<TransformComponent>(grid.Owner);
            Transform.SetWorldPosition(gridXform, pos.Position);
            grid.SetTile(coords ?? TargetCoords, tile);

            if (!MapMan.TryFindGridAt(pos, out grid))
                Assert.Fail("Failed to create grid?");
        });
        await AssertTile(proto, coords);
    }

    protected async Task Delete(EntityUid  uid)
    {
        await Server.WaitPost(() => SEntMan.DeleteEntity(uid));
        await RunTicks(5);
    }

    protected async Task RunTicks(int ticks)
    {
        await PoolManager.RunTicksSync(PairTracker.Pair, ticks);
    }

    protected async Task RunSeconds(float seconds)
        => await RunTicks((int) Math.Ceiling(seconds / TickPeriod));
}
