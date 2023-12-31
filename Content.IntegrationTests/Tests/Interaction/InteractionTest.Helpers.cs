#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Content.Client.Construction;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Components;
using Content.Server.Gravity;
using Content.Server.Item;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Gravity;
using Content.Shared.Item;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
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
            Assert.That(CConSys.TrySpawnGhost(proto, CEntMan.GetCoordinates(TargetCoords), Direction.South, out var clientTarget),
                Is.EqualTo(shouldSucceed));

            if (!shouldSucceed)
                return;

            var comp = CEntMan.GetComponent<ConstructionGhostComponent>(clientTarget!.Value);
            ClientTarget = clientTarget;
            ConstructionGhostId = comp.Owner.Id;
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
        await Server.WaitPost(() => task = SConstruction.TryStartItemConstruction(prototype, SEntMan.GetEntity(Player)));

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
    [MemberNotNull(nameof(Target))]
    protected async Task SpawnTarget(string prototype)
    {
        Target = NetEntity.Invalid;
        await Server.WaitPost(() =>
        {
            Target = SEntMan.GetNetEntity(SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(TargetCoords)));
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
    /// <remarks>
    /// Automatically enables welders.
    /// </remarks>
    protected async Task<NetEntity> PlaceInHands(string id, int quantity = 1, bool enableWelder = true)
    {
        return await PlaceInHands((id, quantity), enableWelder);
    }

    /// <summary>
    /// Place an entity prototype into the players hand. Deletes any currently held entity.
    /// </summary>
    /// <remarks>
    /// Automatically enables welders.
    /// </remarks>
    protected async Task<NetEntity> PlaceInHands(EntitySpecifier entity, bool enableWelder = true)
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
            if (enableWelder && SEntMan.TryGetComponent(item, out itemToggle) && !itemToggle.Activated)
            {
                Assert.That(ItemToggleSys.TryActivate(item, playerEnt, itemToggle: itemToggle));
            }
        });

        await RunTicks(1);
        Assert.That(Hands.ActiveHandEntity, Is.EqualTo(item));
        if (enableWelder && itemToggle != null)
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
    /// <remarks>
    /// Empty strings imply empty hands.
    /// </remarks>
    protected async Task Interact(string id, int quantity = 1, bool shouldSucceed = true, bool awaitDoAfters = true)
    {
        await Interact((id, quantity), shouldSucceed, awaitDoAfters);
    }

    /// <summary>
    /// Place an entity prototype into the players hand and interact with the given entity (or target position)
    /// </summary>
    /// <remarks>
    /// Empty strings imply empty hands.
    /// </remarks>
    protected async Task Interact(EntitySpecifier entity, bool shouldSucceed = true, bool awaitDoAfters = true)
    {
        // For every interaction, we will also examine the entity, just in case this breaks something, somehow.
        // (e.g., servers attempt to assemble construction examine hints).
        if (Target != null)
        {
            await Client.WaitPost(() => ExamineSys.DoExamine(CEntMan.GetEntity(Target.Value)));
        }

        await PlaceInHands(entity);
        await Interact(shouldSucceed, awaitDoAfters);
    }

    /// <summary>
    /// Interact with an entity using the currently held entity.
    /// </summary>
    protected async Task Interact(bool shouldSucceed = true, bool awaitDoAfters = true)
    {
        var clientTarget = ClientTarget;

        if ((clientTarget?.IsValid() != true || CEntMan.Deleted(clientTarget)) && (Target == null || Target.Value.IsValid()))
        {
            await Server.WaitPost(() => InteractSys.UserInteraction(SEntMan.GetEntity(Player), SEntMan.GetCoordinates(TargetCoords), SEntMan.GetEntity(Target)));
            await RunTicks(1);
        }
        else
        {
            // The entity is client-side, so attempt to start construction
            var clientEnt = ClientTarget ?? CEntMan.GetEntity(Target);

            await Client.WaitPost(() => CConSys.TryStartConstruction(clientEnt!.Value));
            await RunTicks(5);
        }

        if (awaitDoAfters)
            await AwaitDoAfters(shouldSucceed);

        await CheckTargetChange(shouldSucceed && awaitDoAfters);
    }

    /// <summary>
    /// Variant of <see cref="InteractUsing"/> that performs several interactions using different entities.
    /// </summary>
    /// <remarks>
    /// Empty strings imply empty hands.
    /// </remarks>
    protected async Task Interact(params EntitySpecifier[] specifiers)
    {
        foreach (var spec in specifiers)
        {
            await Interact(spec);
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
    protected async Task CheckTargetChange(bool shouldSucceed)
    {
        if (Target == null)
            return;

        var target = Target.Value;
        await RunTicks(5);

        if (ClientTarget != null && CEntMan.IsClientSide(ClientTarget.Value))
        {
            Assert.That(CEntMan.Deleted(ClientTarget.Value), Is.EqualTo(shouldSucceed),
                $"Construction ghost was {(shouldSucceed ? "not deleted" : "deleted")}.");

            if (shouldSucceed)
            {
                Assert.That(CTestSystem.Ghosts.TryGetValue(ConstructionGhostId, out var newWeh),
                    $"Failed to get construction entity from ghost Id");

                await Client.WaitPost(() => CLogger.Debug($"Construction ghost {ConstructionGhostId} became entity {newWeh}"));
                Target = newWeh;
            }
        }

        if (STestSystem.EntChanges.TryGetValue(Target.Value, out var newServerWeh))
        {
            await Server.WaitPost(
                () => SLogger.Debug($"Construction entity {Target.Value} changed to {newServerWeh}"));

            Target = newServerWeh;
        }

        if (Target != target)
            await CheckTargetChange(shouldSucceed);
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

        var meta = SEntMan.GetComponent<MetaDataComponent>(SEntMan.GetEntity(target.Value));
        Assert.That(meta.EntityPrototype?.ID, Is.EqualTo(prototype));
    }

    protected void ClientAssertPrototype(string? prototype, EntityUid? target)
    {
        var netEnt = CTestSystem.Ghosts[target.GetHashCode()];
        AssertPrototype(prototype, netEnt);
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
    protected void AssertComp<T>(bool hasComp = true, NetEntity? target = null)
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
        var pos = serverCoords.ToMap(SEntMan, Transform);
        await Server.WaitPost(() =>
        {
            if (MapMan.TryFindGridAt(pos, out _, out var grid))
                tile = grid.GetTileRef(serverCoords).Tile;
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
        expected.ConvertToStacks(ProtoMan, Factory);

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
        spec.ConvertToStack(ProtoMan, Factory);

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

    /// <summary>
    /// Convenience method to get components on the target. Returns SERVER-SIDE components.
    /// </summary>
    protected T Comp<T>(NetEntity? target = null) where T : IComponent
    {
        target ??= Target;
        if (target == null)
            Assert.Fail("No target specified");

        return SEntMan.GetComponent<T>(SEntMan.GetEntity(target!.Value));
    }

    /// <summary>
    /// Set the tile at the target position to some prototype.
    /// </summary>
    protected async Task SetTile(string? proto, NetCoordinates? coords = null, MapGridComponent? grid = null)
    {
        var tile = proto == null
            ? Tile.Empty
            : new Tile(TileMan[proto].TileId);

        var pos = SEntMan.GetCoordinates(coords ?? TargetCoords).ToMap(SEntMan, Transform);

        await Server.WaitPost(() =>
        {
            if (grid != null || MapMan.TryFindGridAt(pos, out var gridUid, out grid))
            {
                grid.SetTile(SEntMan.GetCoordinates(coords ?? TargetCoords), tile);
                return;
            }

            if (proto == null)
                return;

            var gridEnt = MapMan.CreateGridEntity(MapData.MapId);
            grid = gridEnt;
            gridUid = gridEnt;
            var gridXform = SEntMan.GetComponent<TransformComponent>(gridUid);
            Transform.SetWorldPosition(gridXform, pos.Position);
            grid.SetTile(SEntMan.GetCoordinates(coords ?? TargetCoords), tile);

            if (!MapMan.TryFindGridAt(pos, out _, out grid))
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

    protected int SecondsToTicks(float seconds)
    {
        return (int) Math.Ceiling(seconds / TickPeriod);
    }

    protected async Task RunSeconds(float seconds)
    {
        await RunTicks(SecondsToTicks(seconds));
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

        if (!ui.OpenInterfaces.TryGetValue(key, out bui))
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

    #endregion

    #region UI

    /// <summary>
    ///     Presses and releases a button on some client-side window. Will fail if the button cannot be found.
    /// </summary>
    protected async Task ClickControl<TWindow>(string name) where TWindow : BaseWindow
    {
        await ClickControl(GetControl<TWindow, Control>(name));
    }

    /// <summary>
    ///     Simulates a click and release at the center of some UI Constrol.
    /// </summary>
    protected async Task ClickControl(Control control)
    {
        var screenCoords = new ScreenCoordinates(
            control.GlobalPixelPosition + control.PixelSize / 2,
            control.Window?.Id ?? default);

        var relativePos = screenCoords.Position / control.UIScale - control.GlobalPosition;
        var relativePixelPos = screenCoords.Position - control.GlobalPixelPosition;

        var args = new GUIBoundKeyEventArgs(
            EngineKeyFunctions.UIClick,
            BoundKeyState.Down,
            screenCoords,
            default,
            relativePos,
            relativePixelPos);

        await Client.DoGuiEvent(control, args);
        await RunTicks(1);

        args = new GUIBoundKeyEventArgs(
            EngineKeyFunctions.UIClick,
            BoundKeyState.Up,
            screenCoords,
            default,
            relativePos,
            relativePixelPos);

        await Client.DoGuiEvent(control, args);
        await RunTicks(1);
    }

    /// <summary>
    ///     Attempts to find a control on some client-side window. Will fail if the control cannot be found.
    /// </summary>
    protected TControl GetControl<TWindow, TControl>(string name)
        where TWindow : BaseWindow
        where TControl : Control
    {
        var control = GetControl<TWindow>(name);
        Assert.That(control.GetType().IsAssignableTo(typeof(TControl)));
        return (TControl) control;
    }

    protected Control GetControl<TWindow>(string name) where TWindow : BaseWindow
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var field = typeof(TWindow).GetField(name, flags);
        var prop = typeof(TWindow).GetProperty(name, flags);

        if (field == null && prop == null)
        {
            Assert.Fail($"Window {typeof(TWindow).Name} does not have a field or property named {name}");
            return default!;
        }

        var window = GetWindow<TWindow>();
        var fieldOrProp = field?.GetValue(window) ?? prop?.GetValue(window);

        if (fieldOrProp is not Control control)
        {
            Assert.Fail($"{name} was null or was not a control.");
            return default!;
        }

        return control;
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
        var target = uid ?? MapData.GridUid;
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
            var atmos = SEntMan.EnsureComponent<MapAtmosphereComponent>(target);
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int) Gas.Oxygen] = 21.824779f;
            moles[(int) Gas.Nitrogen] = 82.10312f;
            atmosSystem.SetMapAtmosphere(target, false, new GasMixture(2500)
            {
                Temperature = 293.15f,
                Moles = moles,
            }, atmos);
        });
    }

    #endregion

    #region Inputs

    /// <summary>
    ///     Make the client press and then release a key. This assumes the key is currently released.
    /// </summary>
    protected async Task PressKey(
        BoundKeyFunction key,
        int ticks = 1,
        NetCoordinates? coordinates = null,
        NetEntity cursorEntity = default)
    {
        await SetKey(key, BoundKeyState.Down, coordinates, cursorEntity);
        await RunTicks(ticks);
        await SetKey(key, BoundKeyState.Up, coordinates, cursorEntity);
        await RunTicks(1);
    }

    /// <summary>
    ///     Make the client press or release a key
    /// </summary>
    protected async Task SetKey(
        BoundKeyFunction key,
        BoundKeyState state,
        NetCoordinates? coordinates = null,
        NetEntity cursorEntity = default)
    {
        var coords = coordinates ?? TargetCoords;
        ScreenCoordinates screen = default;

        var funcId = InputManager.NetworkBindMap.KeyFunctionID(key);
        var message = new ClientFullInputCmdMessage(CTiming.CurTick, CTiming.TickFraction, funcId)
        {
            State = state,
            Coordinates = CEntMan.GetCoordinates(coords),
            ScreenCoordinates = screen,
            Uid = CEntMan.GetEntity(cursorEntity),
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
