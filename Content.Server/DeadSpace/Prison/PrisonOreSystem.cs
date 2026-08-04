using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeadSpace.Prison.Components;
using Content.Server.GameTicking;
using Content.Server.Mining;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Events;
using Content.Server.Stack;
using Content.Shared.Cargo.Components;
using Content.Shared.DeadSpace.Prison;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Prison;

public sealed class PrisonOreSystem : EntitySystem
{
    private const string ShipmentPrototype = "CratePrisonOreShipment";

    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly ITaskManager _tasks = default!;
    [Dependency] private readonly PrisonSystem _prison = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    private readonly HashSet<EntityUid> _pendingStacks = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrisonOreProcessorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PrisonOreProcessorComponent, DumpEvent>(OnDump);
        SubscribeLocalEvent<PrisonOreProcessorComponent, ExaminedEvent>(OnProcessorExamined);
        SubscribeLocalEvent<PrisonMinedOreComponent, StackSplitEvent>(OnStackSplit);
        SubscribeLocalEvent<PrisonMinedOreComponent, StackMergedEvent>(OnStackMerged);
        SubscribeLocalEvent<PrisonOreShipmentComponent, StackSplitEvent>(OnShipmentStackSplit);
        SubscribeLocalEvent<PrisonOreShipmentComponent, StackMergedEvent>(OnShipmentStackMerged);
        SubscribeLocalEvent<PrisonOreShipmentComponent, ExaminedEvent>(OnShipmentExamined);
        SubscribeLocalEvent<PrisonOreProcessorComponent, PrisonOreBoxDepositEvent>(OnOreBoxDeposit);
        SubscribeLocalEvent<CargoShuttleComponent, FTLStartedEvent>(OnCargoFtlStarted);
        SubscribeLocalEvent<CargoShuttleComponent, FTLCompletedEvent>(OnCargoFtlCompleted);
        SubscribeLocalEvent<OreMinedEvent>(OnOreMined);
    }

    public void SetEligibleUnits(EntityUid ore, int units)
    {
        if (!TryComp<StackComponent>(ore, out var stack))
            return;

        var eligible = Math.Clamp(units, 0, stack.Count);
        if (eligible == 0)
        {
            RemCompDeferred<PrisonMinedOreComponent>(ore);
            return;
        }

        EnsureComp<PrisonMinedOreComponent>(ore).EligibleUnits = eligible;
    }

    internal void SetShipmentTracking(
        EntityUid oreEntity,
        ProtoId<StackPrototype> ore,
        int units,
        NetUserId userId,
        int banId,
        long reductionTicks)
    {
        var shipment = EnsureComp<PrisonOreShipmentComponent>(oreEntity);
        shipment.Ores[ore] = units;
        AddContribution(shipment, userId, banId, reductionTicks);
        UpdateShipmentDescription(oreEntity, shipment);
    }

    private void OnOreMined(ref OreMinedEvent args)
    {
        if (!TryComp(args.Vein, out TransformComponent? veinXform) ||
            !_prison.IsPrisonMap(veinXform.MapID) ||
            !TryComp<StackComponent>(args.Ore, out var stack))
        {
            return;
        }

        SetEligibleUnits(args.Ore, stack.Count);
    }

    private void OnStackSplit(Entity<PrisonMinedOreComponent> ent, ref StackSplitEvent args)
    {
        if (args.Amount <= 0 || ent.Comp.EligibleUnits <= 0)
            return;

        var moved = Math.Min(ent.Comp.EligibleUnits, args.Amount);
        ent.Comp.EligibleUnits -= moved;
        EnsureComp<PrisonMinedOreComponent>(args.NewId).EligibleUnits = moved;

        if (ent.Comp.EligibleUnits <= 0)
            RemCompDeferred<PrisonMinedOreComponent>(ent.Owner);
    }

    private void OnStackMerged(Entity<PrisonMinedOreComponent> ent, ref StackMergedEvent args)
    {
        if (args.Amount <= 0 || ent.Comp.EligibleUnits <= 0)
            return;

        var donorCount = TryComp<StackComponent>(ent.Owner, out var donor) ? donor.Count : args.Amount;
        var moved = Math.Min(Math.Min(ent.Comp.EligibleUnits, donorCount), args.Amount);
        if (moved <= 0)
            return;

        ent.Comp.EligibleUnits -= moved;
        var recipient = EnsureComp<PrisonMinedOreComponent>(args.Recipient);
        recipient.EligibleUnits += moved;

        if (ent.Comp.EligibleUnits <= 0)
            RemCompDeferred<PrisonMinedOreComponent>(ent.Owner);
    }

    private void OnShipmentStackSplit(Entity<PrisonOreShipmentComponent> ent, ref StackSplitEvent args)
    {
        if (ent.Comp.Delivered || args.Amount <= 0)
            return;

        var trackedUnits = ent.Comp.Ores.Values.Sum();
        var movedUnits = Math.Min(trackedUnits, args.Amount);
        if (movedUnits <= 0)
            return;

        var recipient = EnsureComp<PrisonOreShipmentComponent>(args.NewId);
        recipient.InTransit = ent.Comp.InTransit;
        TransferShipmentUnits(ent.Comp, recipient, movedUnits, trackedUnits);
        if (ent.Comp.Ores.Count == 0)
            RemCompDeferred<PrisonOreShipmentComponent>(ent.Owner);
        else
            UpdateShipmentDescription(ent.Owner, ent.Comp);
        UpdateShipmentDescription(args.NewId, recipient);
    }

    private void OnShipmentStackMerged(Entity<PrisonOreShipmentComponent> ent, ref StackMergedEvent args)
    {
        if (ent.Comp.Delivered || args.Amount <= 0)
            return;

        var trackedUnits = ent.Comp.Ores.Values.Sum();
        var movedUnits = Math.Min(trackedUnits, args.Amount);
        if (movedUnits <= 0)
            return;

        var recipient = EnsureComp<PrisonOreShipmentComponent>(args.Recipient);
        recipient.Delivered = false;
        recipient.InTransit |= ent.Comp.InTransit;
        TransferShipmentUnits(ent.Comp, recipient, movedUnits, trackedUnits);
        if (ent.Comp.Ores.Count == 0)
            RemCompDeferred<PrisonOreShipmentComponent>(ent.Owner);
        else
            UpdateShipmentDescription(ent.Owner, ent.Comp);
        UpdateShipmentDescription(args.Recipient, recipient);
    }

    private void TransferShipmentUnits(
        PrisonOreShipmentComponent source,
        PrisonOreShipmentComponent recipient,
        int movedUnits,
        int sourceUnits)
    {
        if (movedUnits <= 0 || sourceUnits <= 0)
            return;

        var remaining = movedUnits;
        foreach (var (ore, amount) in source.Ores.ToArray())
        {
            var moved = Math.Min(amount, remaining);
            if (moved <= 0)
                continue;

            source.Ores[ore] -= moved;
            if (source.Ores[ore] <= 0)
                source.Ores.Remove(ore);

            recipient.Ores[ore] = recipient.Ores.GetValueOrDefault(ore) + moved;
            remaining -= moved;
            if (remaining <= 0)
                break;
        }

        foreach (var contribution in source.Contributions.ToArray())
        {
            if (contribution.Processing || contribution.ReductionTicks <= 0)
                continue;

            var movedTicks = movedUnits == sourceUnits
                ? contribution.ReductionTicks
                : (long) ((decimal) contribution.ReductionTicks * movedUnits / sourceUnits);
            if (movedTicks <= 0)
                continue;

            contribution.ReductionTicks -= movedTicks;
            AddContribution(recipient, contribution.UserId, contribution.BanId, movedTicks);
            if (contribution.ReductionTicks <= 0)
                source.Contributions.Remove(contribution);
        }
    }

    private void OnInteractUsing(Entity<PrisonOreProcessorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<StorageComponent>(args.Used, out var storage))
        {
            args.Handled = true;
            BeginDeposit(ent, args.User, storage.Container.ContainedEntities.ToArray());
            return;
        }

        if (!HasComp<StackComponent>(args.Used))
            return;

        args.Handled = true;
        BeginDeposit(ent, args.User, [args.Used]);
    }

    private void OnDump(Entity<PrisonOreProcessorComponent> ent, ref DumpEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.PlaySound = false;
        BeginDeposit(ent, args.User, args.DumpQueue.ToArray());
    }

    private void OnOreBoxDeposit(Entity<PrisonOreProcessorComponent> ent, ref PrisonOreBoxDepositEvent args)
    {
        if (!TryComp<StorageComponent>(args.Box, out var storage))
            return;

        BeginDeposit(ent, args.User, storage.Container.ContainedEntities.ToArray());
    }

    private void BeginDeposit(Entity<PrisonOreProcessorComponent> ent, EntityUid user, IReadOnlyCollection<EntityUid> candidates)
    {
        if (!_players.TryGetSessionByEntity(user, out var session) || !_prison.IsEntityPrisoner(user))
        {
            _popup.PopupEntity(Loc.GetString("prison-ore-not-prisoner"), ent.Owner, user);
            return;
        }

        if (!this.IsPowered(ent.Owner, EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("prison-ore-unpowered"), ent.Owner, user);
            return;
        }

        var stacks = candidates
            .Where(uid => !_pendingStacks.Contains(uid) &&
                          TryComp<PrisonMinedOreComponent>(uid, out var mined) && mined.EligibleUnits > 0 &&
                          TryComp<StackComponent>(uid, out var stack) && !stack.Unlimited &&
                          ent.Comp.OreValues.ContainsKey(stack.StackTypeId))
            .Distinct()
            .ToArray();

        if (stacks.Length == 0)
        {
            _popup.PopupEntity(Loc.GetString("prison-ore-no-eligible-ore"), ent.Owner, user);
            return;
        }

        foreach (var stack in stacks)
            _pendingStacks.Add(stack);

        ValidateAndDeposit(ent.Owner, user, session.UserId, stacks);
    }

    private async void ValidateAndDeposit(EntityUid processor, EntityUid user, NetUserId userId, EntityUid[] stacks)
    {
        PrisonSentence? sentence = null;
        try
        {
            sentence = await _prison.GetReducibleSentence(userId);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to validate prison ore deposit for {userId}: {e}");
        }

        _tasks.RunOnMainThread(() => FinishDeposit(processor, user, userId, stacks, sentence));
    }

    private void FinishDeposit(
        EntityUid processor,
        EntityUid user,
        NetUserId userId,
        EntityUid[] stacks,
        PrisonSentence? sentence)
    {
        foreach (var stack in stacks)
            _pendingStacks.Remove(stack);

        if (!Exists(processor) || !Exists(user) ||
            !TryComp<PrisonOreProcessorComponent>(processor, out var component) ||
            !_players.TryGetSessionByEntity(user, out var currentSession) ||
            currentSession.UserId != userId ||
            !_prison.IsEntityPrisoner(user))
        {
            return;
        }

        if (sentence == null)
        {
            _popup.PopupEntity(Loc.GetString("prison-ore-no-temporary-sentence"), processor, user);
            return;
        }

        if (!this.IsPowered(processor, EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("prison-ore-unpowered"), processor, user);
            return;
        }

        var ores = new Dictionary<ProtoId<StackPrototype>, int>();
        long points = 0;
        foreach (var uid in stacks)
        {
            if (!TryComp<PrisonMinedOreComponent>(uid, out var mined) ||
                !TryComp<StackComponent>(uid, out var stack) ||
                stack.Unlimited ||
                !component.OreValues.TryGetValue(stack.StackTypeId, out var value))
            {
                continue;
            }

            var units = Math.Min(mined.EligibleUnits, stack.Count);
            if (units <= 0)
                continue;

            ores[stack.StackTypeId] = ores.GetValueOrDefault(stack.StackTypeId) + units;
            points += (long) units * value;
        }

        if (points <= 0 || component.PointsPerSecond <= 0)
        {
            _popup.PopupEntity(Loc.GetString("prison-ore-no-eligible-ore"), processor, user);
            return;
        }

        if (!TryCreatePhysicalShipment(
                ores,
                userId,
                sentence.Value.BanId,
                component,
                out var reductionTicks))
        {
            _popup.PopupEntity(Loc.GetString("prison-ore-no-cargo-space"), processor, user);
            return;
        }

        var consumed = 0;
        foreach (var uid in stacks)
        {
            if (!TryComp<PrisonMinedOreComponent>(uid, out var mined) ||
                !TryComp<StackComponent>(uid, out var stack) ||
                !ores.TryGetValue(stack.StackTypeId, out _) ||
                !component.OreValues.ContainsKey(stack.StackTypeId))
            {
                continue;
            }

            var units = Math.Min(mined.EligibleUnits, stack.Count);
            if (units <= 0)
                continue;

            mined.EligibleUnits -= units;
            consumed += units;
            _stack.ReduceCount((uid, stack), units);
            if (mined.EligibleUnits <= 0 && Exists(uid))
                RemCompDeferred<PrisonMinedOreComponent>(uid);
        }

        var seconds = TimeSpan.FromTicks(reductionTicks).TotalSeconds;
        _popup.PopupEntity(
            Loc.GetString("prison-ore-deposit-accepted", ("units", consumed), ("seconds", seconds.ToString("N1"))),
            processor,
            user);
    }

    internal bool TryCreatePhysicalShipment(
        IReadOnlyDictionary<ProtoId<StackPrototype>, int> ores,
        NetUserId userId,
        int banId,
        PrisonOreProcessorComponent processor,
        out long reductionTicks)
    {
        reductionTicks = 0;
        if (!TryGetCargoSpawnCoordinates(out var coordinates))
            return false;

        var totalUnits = ores.Values.Sum();
        EntityUid? crate = null;
        if (totalUnits >= Math.Max(1, processor.CrateMinimumUnits))
        {
            crate = Spawn(ShipmentPrototype, coordinates);
            _metadata.SetEntityDescription(
                crate.Value,
                Loc.GetString("prison-ore-crate-description", ("units", totalUnits)));
        }

        var spawned = new List<EntityUid>();
        foreach (var (ore, amount) in ores)
        {
            if (!processor.OreValues.TryGetValue(ore, out var value))
                continue;

            foreach (var stackUid in _stack.SpawnMultipleAtPosition(ore, amount, coordinates))
            {
                if (!TryComp<StackComponent>(stackUid, out var stack))
                    continue;

                spawned.Add(stackUid);
                var shipment = EnsureComp<PrisonOreShipmentComponent>(stackUid);
                shipment.Ores[ore] = stack.Count;
                var stackTicks = checked((long) stack.Count * value * TimeSpan.TicksPerSecond /
                                         processor.PointsPerSecond);
                AddContribution(shipment, userId, banId, stackTicks);
                reductionTicks = checked(reductionTicks + stackTicks);
                UpdateShipmentDescription(stackUid, shipment);

                if (crate != null && !_entityStorage.Insert(stackUid, crate.Value))
                {
                    foreach (var entity in spawned)
                        QueueDel(entity);

                    QueueDel(crate.Value);
                    reductionTicks = 0;
                    return false;
                }
            }
        }

        if (spawned.Count > 0)
            return true;

        if (crate != null)
            QueueDel(crate.Value);

        return false;
    }

    private static void AddContribution(
        PrisonOreShipmentComponent shipment,
        NetUserId userId,
        int banId,
        long reductionTicks)
    {
        if (reductionTicks <= 0)
            return;

        var contribution = shipment.Contributions.FirstOrDefault(c =>
            c.UserId == userId && c.BanId == banId && !c.Processing);
        if (contribution == null)
            shipment.Contributions.Add(new PrisonOreContribution(userId, banId, reductionTicks));
        else
            contribution.ReductionTicks = checked(contribution.ReductionTicks + reductionTicks);
    }

    internal bool TryGetCargoSpawnCoordinates(out EntityCoordinates coordinates)
    {
        var shuttleQuery = EntityQueryEnumerator<CargoShuttleComponent, MapGridComponent>();
        while (shuttleQuery.MoveNext(out var shuttle, out _, out var grid))
        {
            if (TryGetCargoSpawnCoordinates(shuttle, grid, out coordinates))
            {
                return true;
            }
        }

        coordinates = EntityCoordinates.Invalid;
        return false;
    }

    private bool TryGetCargoSpawnCoordinates(
        EntityUid shuttle,
        MapGridComponent grid,
        out EntityCoordinates coordinates)
    {
        var query = EntityQueryEnumerator<TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var xform, out var metadata))
        {
            if (xform.GridUid != shuttle || !xform.Anchored)
                continue;

            var isCargoPallet = metadata.EntityPrototype?.ID == "CargoPallet";
            if (!isCargoPallet &&
                (!TryComp<CargoPalletComponent>(uid, out var pallet) ||
                 (pallet.PalletType & BuySellType.Buy) == 0))
            {
                continue;
            }

            var candidate = new EntityCoordinates(shuttle, xform.LocalPosition);
            if (_lookup.GetEntitiesInRange(candidate, 0.4f, LookupFlags.Dynamic).Count != 0)
                continue;

            coordinates = candidate;
            return true;
        }

        foreach (var tile in _map.GetAllTiles(shuttle, grid))
        {
            if (tile.Tile.IsEmpty)
                continue;

            var candidate = new EntityCoordinates(shuttle, tile.GridIndices + new System.Numerics.Vector2(0.5f));
            var blocked = false;
            foreach (var nearby in _lookup.GetEntitiesInRange(
                         candidate,
                         0.4f,
                         LookupFlags.Dynamic | LookupFlags.Static))
            {
                if (TryComp<PhysicsComponent>(nearby, out var physics) && physics.CanCollide)
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked)
                continue;

            coordinates = candidate;
            return true;
        }

        coordinates = EntityCoordinates.Invalid;
        return false;
    }

    private void OnCargoFtlStarted(Entity<CargoShuttleComponent> ent, ref FTLStartedEvent args)
    {
        SetShipmentTransitState(ent.Owner, true);
    }

    private void OnCargoFtlCompleted(Entity<CargoShuttleComponent> ent, ref FTLCompletedEvent args)
    {
        var arrivedAtStation = Transform(ent.Owner).MapID == _ticker.DefaultMap;
        var query = EntityQueryEnumerator<PrisonOreShipmentComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var shipment, out var xform))
        {
            if (xform.GridUid != ent.Owner || shipment.Delivered)
                continue;

            shipment.InTransit = false;
            if (arrivedAtStation)
                DeliverShipment((uid, shipment));
        }
    }

    private void SetShipmentTransitState(EntityUid shuttle, bool inTransit)
    {
        var query = EntityQueryEnumerator<PrisonOreShipmentComponent, TransformComponent>();
        while (query.MoveNext(out _, out var shipment, out var xform))
        {
            if (xform.GridUid == shuttle && !shipment.Delivered)
                shipment.InTransit = inTransit;
        }
    }

    private void DeliverShipment(Entity<PrisonOreShipmentComponent> ent)
    {
        ent.Comp.Delivered = true;
        UpdateShipmentDescription(ent.Owner, ent.Comp);

        foreach (var contribution in ent.Comp.Contributions)
        {
            if (contribution.Processing || contribution.ReductionTicks <= 0)
                continue;

            contribution.Processing = true;
            ApplyContribution(contribution);
        }
    }

    private async void ApplyContribution(PrisonOreContribution contribution)
    {
        TimeSpan applied = TimeSpan.Zero;
        try
        {
            applied = await _prison.TryReduceSentence(
                contribution.UserId,
                contribution.BanId,
                TimeSpan.FromTicks(contribution.ReductionTicks));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to apply prison ore shipment reward for {contribution.UserId}: {e}");
        }

        _tasks.RunOnMainThread(() =>
        {
            if (applied <= TimeSpan.Zero)
                return;

            _prison.RefreshPrisonBanState();
            if (_players.TryGetSessionById(contribution.UserId, out var session))
            {
                _chat.DispatchServerMessage(
                    session,
                    Loc.GetString("prison-ore-reward-message", ("seconds", applied.TotalSeconds.ToString("N1"))));
            }
        });
    }

    private void OnProcessorExamined(Entity<PrisonOreProcessorComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("prison-ore-processor-examine"));
    }

    private void OnShipmentExamined(Entity<PrisonOreShipmentComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var units = ent.Comp.Ores.Values.Sum();
        args.PushMarkup(Loc.GetString(
            ent.Comp.Delivered ? "prison-ore-shipment-delivered" : "prison-ore-shipment-pending",
            ("units", units)));
    }

    private void UpdateShipmentDescription(EntityUid uid, PrisonOreShipmentComponent shipment)
    {
        if (!Exists(uid))
            return;

        _metadata.SetEntityDescription(
            uid,
            Loc.GetString(
                shipment.Delivered ? "prison-ore-loose-delivered-description" : "prison-ore-loose-description",
                ("units", shipment.Ores.Values.Sum())));
    }
}
