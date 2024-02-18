using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Labels;
using Content.Server.NameIdentifier;
using Content.Server.Paper;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.NameIdentifier;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly NameIdentifierSystem _nameIdentifier = default!;

    [ValidatePrototypeId<NameIdentifierGroupPrototype>]
    private const string BountyNameIdentifierGroup = "Bounty";

    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private EntityQuery<CargoBountyLabelComponent> _bountyLabelQuery;

    private void InitializeBounty()
    {
        SubscribeLocalEvent<CargoBountyConsoleComponent, BoundUIOpenedEvent>(OnBountyConsoleOpened);
        SubscribeLocalEvent<CargoBountyConsoleComponent, BountyPrintLabelMessage>(OnPrintLabelMessage);
        SubscribeLocalEvent<CargoBountyLabelComponent, PriceCalculationEvent>(OnGetBountyPrice);
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
        SubscribeLocalEvent<StationCargoBountyDatabaseComponent, MapInitEvent>(OnMapInit);

        _stackQuery = GetEntityQuery<StackComponent>();
        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        _bountyLabelQuery = GetEntityQuery<CargoBountyLabelComponent>();
    }

    private void OnBountyConsoleOpened(EntityUid uid, CargoBountyConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (_station.GetOwningStation(uid) is not { } station ||
            !TryComp<StationCargoBountyDatabaseComponent>(station, out var bountyDb))
            return;

        _uiSystem.TrySetUiState(uid, CargoConsoleUiKey.Bounty, new CargoBountyConsoleState(bountyDb.Bounties));
    }

    private void OnPrintLabelMessage(EntityUid uid, CargoBountyConsoleComponent component, BountyPrintLabelMessage args)
    {
        if (_timing.CurTime < component.NextPrintTime)
            return;

        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (!TryGetBountyFromId(station, args.BountyId, out var bounty))
            return;

        var label = Spawn(component.BountyLabelId, Transform(uid).Coordinates);
        component.NextPrintTime = _timing.CurTime + component.PrintDelay;
        SetupBountyLabel(label, bounty.Value);
        _audio.PlayPvs(component.PrintSound, uid);
    }

    public void SetupBountyLabel(EntityUid uid, CargoBountyData bounty, PaperComponent? paper = null, CargoBountyLabelComponent? label = null)
    {
        if (!Resolve(uid, ref paper, ref label) || !_protoMan.TryIndex<CargoBountyPrototype>(bounty.Bounty, out var prototype))
            return;

        label.Id = bounty.Id;
        var msg = new FormattedMessage();
        msg.AddText(Loc.GetString("bounty-manifest-header", ("id", bounty.Id)));
        msg.PushNewline();
        msg.AddText(Loc.GetString("bounty-manifest-list-start"));
        msg.PushNewline();
        foreach (var entry in prototype.Entries)
        {
            msg.AddMarkup($"- {Loc.GetString("bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name)))}");
            msg.PushNewline();
        }
        _paperSystem.SetContent(uid, msg.ToMarkup(), paper);
    }

    /// <summary>
    /// Bounties do not sell for any currency. The reward for a bounty is
    /// calculated after it is sold separately from the selling system.
    /// </summary>
    private void OnGetBountyPrice(EntityUid uid, CargoBountyLabelComponent component, ref PriceCalculationEvent args)
    {
        if (args.Handled || component.Calculating)
            return;

        // make sure this label was actually applied to a crate.
        if (!_container.TryGetContainingContainer(uid, out var container) || container.ID != LabelSystem.ContainerName)
            return;

        if (_station.GetOwningStation(uid) is not { } station || !TryComp<StationCargoBountyDatabaseComponent>(station, out var database))
            return;

        if (database.CheckedBounties.Contains(component.Id))
            return;

        if (!TryGetBountyFromId(station, component.Id, out var bounty, database))
            return;

        if (!_protoMan.TryIndex(bounty.Value.Bounty, out var bountyPrototype) ||
            !IsBountyComplete(container.Owner, bountyPrototype))
            return;

        database.CheckedBounties.Add(component.Id);
        args.Handled = true;

        component.Calculating = true;
        args.Price = bountyPrototype.Reward - _pricing.GetPrice(container.Owner);
        component.Calculating = false;
    }

    private void OnSold(ref EntitySoldEvent args)
    {
        foreach (var sold in args.Sold)
        {
            if (!TryGetBountyLabel(sold, out _, out var component))
                continue;

            if (!TryGetBountyFromId(args.Station, component.Id, out var bounty))
                continue;

            if (!IsBountyComplete(sold, bounty.Value))
                continue;

            TryRemoveBounty(args.Station, bounty.Value);
            FillBountyDatabase(args.Station);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"Bounty \"{bounty.Value.Bounty}\" (id:{bounty.Value.Id}) was fulfilled");
        }
    }

    private bool TryGetBountyLabel(EntityUid uid,
        [NotNullWhen(true)] out EntityUid? labelEnt,
        [NotNullWhen(true)] out CargoBountyLabelComponent? labelComp)
    {
        labelEnt = null;
        labelComp = null;
        if (!_containerQuery.TryGetComponent(uid, out var containerMan))
            return false;

        // make sure this label was actually applied to a crate.
        if (!_container.TryGetContainer(uid, LabelSystem.ContainerName, out var container, containerMan))
            return false;

        if (container.ContainedEntities.FirstOrNull() is not { } label ||
            !_bountyLabelQuery.TryGetComponent(label, out var component))
            return false;

        labelEnt = label;
        labelComp = component;
        return true;
    }

    private void OnMapInit(EntityUid uid, StationCargoBountyDatabaseComponent component, MapInitEvent args)
    {
        FillBountyDatabase(uid, component);
    }

    /// <summary>
    /// Fills up the bounty database with random bounties.
    /// </summary>
    public void FillBountyDatabase(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        while (component.Bounties.Count < component.MaxBounties)
        {
            if (!TryAddBounty(uid, component))
                break;
        }

        UpdateBountyConsoles();
    }

    public void RerollBountyDatabase(Entity<StationCargoBountyDatabaseComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.Bounties.Clear();
        FillBountyDatabase(entity);
    }

    public bool IsBountyComplete(EntityUid container, EntityUid? station, out HashSet<EntityUid> bountyEntities)
    {
        if (!TryGetBountyLabel(container, out _, out var component))
        {
            bountyEntities = new();
            return false;
        }

        station ??= _station.GetOwningStation(container);
        if (station == null)
        {
            bountyEntities = new();
            return false;
        }

        if (!TryGetBountyFromId(station.Value, component.Id, out var bounty))
        {
            bountyEntities = new();
            return false;
        }

        return IsBountyComplete(container, bounty.Value, out bountyEntities);
    }

    public bool IsBountyComplete(EntityUid container, CargoBountyData data)
    {
        return IsBountyComplete(container, data, out _);
    }

    public bool IsBountyComplete(EntityUid container, CargoBountyData data,  out HashSet<EntityUid> bountyEntities)
    {
        if (!_protoMan.TryIndex(data.Bounty, out var proto))
        {
            bountyEntities = new();
            return false;
        }

        return IsBountyComplete(container, proto.Entries, out bountyEntities);
    }

    public bool IsBountyComplete(EntityUid container, string id)
    {
        if (!_protoMan.TryIndex<CargoBountyPrototype>(id, out var proto))
            return false;

        return IsBountyComplete(container, proto.Entries);
    }

    public bool IsBountyComplete(EntityUid container, CargoBountyPrototype prototype)
    {
        return IsBountyComplete(container, prototype.Entries);
    }

    public bool IsBountyComplete(EntityUid container, IEnumerable<CargoBountyItemEntry> entries)
    {
        return IsBountyComplete(container, entries, out _);
    }

    public bool IsBountyComplete(EntityUid container, IEnumerable<CargoBountyItemEntry> entries, out HashSet<EntityUid> bountyEntities)
    {
        return IsBountyComplete(GetBountyEntities(container), entries, out bountyEntities);
    }

    public bool IsBountyComplete(HashSet<EntityUid> entities, IEnumerable<CargoBountyItemEntry> entries, out HashSet<EntityUid> bountyEntities)
    {
        bountyEntities = new();

        foreach (var entry in entries)
        {
            var count = 0;

            // store entities that already satisfied an
            // entry so we don't double-count them.
            var temp = new HashSet<EntityUid>();
            foreach (var entity in entities)
            {
                if (!entry.Whitelist.IsValid(entity, EntityManager))
                    continue;

                count += _stackQuery.CompOrNull(entity)?.Count ?? 1;
                temp.Add(entity);

                if (count >= entry.Amount)
                    break;
            }

            if (count < entry.Amount)
                return false;

            foreach (var ent in temp)
            {
                entities.Remove(ent);
                bountyEntities.Add(ent);
            }
        }

        return true;
    }

    private HashSet<EntityUid> GetBountyEntities(EntityUid uid)
    {
        var entities = new HashSet<EntityUid>
        {
            uid
        };
        if (!TryComp<ContainerManagerComponent>(uid, out var containers))
            return entities;

        foreach (var container in containers.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                if (_bountyLabelQuery.HasComponent(ent))
                    continue;

                var children = GetBountyEntities(ent);
                foreach (var child in children)
                {
                 entities.Add(child);
                }
            }
        }

        return entities;
    }

    [PublicAPI]
    public bool TryAddBounty(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // todo: consider making the cargo bounties weighted.
        var allBounties = _protoMan.EnumeratePrototypes<CargoBountyPrototype>().ToList();
        var filteredBounties = new List<CargoBountyPrototype>();
        foreach (var proto in allBounties)
        {
            if (component.Bounties.Any(b => b.Bounty == proto.ID))
                continue;
            filteredBounties.Add(proto);
        }

        var pool = filteredBounties.Count == 0 ? allBounties : filteredBounties;
        var bounty = _random.Pick(pool);
        return TryAddBounty(uid, bounty, component);
    }

    [PublicAPI]
    public bool TryAddBounty(EntityUid uid, string bountyId, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!_protoMan.TryIndex<CargoBountyPrototype>(bountyId, out var bounty))
        {
            return false;
        }

        return TryAddBounty(uid, bounty, component);
    }

    public bool TryAddBounty(EntityUid uid, CargoBountyPrototype bounty, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Bounties.Count >= component.MaxBounties)
            return false;

        _nameIdentifier.GenerateUniqueName(uid, BountyNameIdentifierGroup, out var randomVal);
        component.Bounties.Add(new CargoBountyData(bounty, randomVal));
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Added bounty \"{bounty.ID}\" (id:{component.TotalBounties}) to station {ToPrettyString(uid)}");
        component.TotalBounties++;
        return true;
    }

    [PublicAPI]
    public bool TryRemoveBounty(EntityUid uid, string dataId, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!TryGetBountyFromId(uid, dataId, out var data, component))
            return false;

        return TryRemoveBounty(uid, data.Value, component);
    }

    public bool TryRemoveBounty(EntityUid uid, CargoBountyData data, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        for (var i = 0; i < component.Bounties.Count; i++)
        {
            if (component.Bounties[i].Id == data.Id)
            {
                component.Bounties.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool TryGetBountyFromId(
        EntityUid uid,
        string id,
        [NotNullWhen(true)] out CargoBountyData? bounty,
        StationCargoBountyDatabaseComponent? component = null)
    {
        bounty = null;
        if (!Resolve(uid, ref component))
            return false;

        foreach (var bountyData in component.Bounties)
        {
            if (bountyData.Id != id)
                continue;
            bounty = bountyData;
            break;
        }

        return bounty != null;
    }

    public void UpdateBountyConsoles()
    {
        var query = EntityQueryEnumerator<CargoBountyConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_station.GetOwningStation(uid) is not { } station ||
                !TryComp<StationCargoBountyDatabaseComponent>(station, out var db))
                continue;

            _uiSystem.TrySetUiState(uid, CargoConsoleUiKey.Bounty, new CargoBountyConsoleState(db.Bounties), ui: ui);
        }
    }

    private void UpdateBounty()
    {
        var query = EntityQueryEnumerator<StationCargoBountyDatabaseComponent>();
        while (query.MoveNext(out var bountyDatabase))
        {
            bountyDatabase.CheckedBounties.Clear();
        }
    }
}
