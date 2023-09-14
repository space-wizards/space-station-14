using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Labels;
using Content.Server.Paper;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;

    private void InitializeBounty()
    {
        SubscribeLocalEvent<CargoBountyConsoleComponent, BoundUIOpenedEvent>(OnBountyConsoleOpened);
        SubscribeLocalEvent<CargoBountyConsoleComponent, BountyPrintLabelMessage>(OnPrintLabelMessage);
        SubscribeLocalEvent<CargoBountyLabelComponent, PriceCalculationEvent>(OnGetBountyPrice);
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
        SubscribeLocalEvent<StationCargoBountyDatabaseComponent, MapInitEvent>(OnMapInit);
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

        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (!TryGetBountyFromId(station, component.Id, out var bounty))
            return;

        if (!_protoMan.TryIndex<CargoBountyPrototype>(bounty.Value.Bounty, out var bountyProtoype) ||!IsBountyComplete(container.Owner, bountyProtoype))
            return;
        args.Handled = true;

        component.Calculating = true;
        args.Price = bountyProtoype.Reward - _pricing.GetPrice(container.Owner);
        component.Calculating = false;
    }

    private void OnSold(ref EntitySoldEvent args)
    {
        var containerQuery = GetEntityQuery<ContainerManagerComponent>();
        var labelQuery = GetEntityQuery<CargoBountyLabelComponent>();
        foreach (var sold in args.Sold)
        {
            if (!containerQuery.TryGetComponent(sold, out var containerMan))
                continue;

            // make sure this label was actually applied to a crate.
            if (!_container.TryGetContainer(sold, LabelSystem.ContainerName, out var container, containerMan))
                continue;

            if (container.ContainedEntities.FirstOrNull() is not { } label ||
                !labelQuery.TryGetComponent(label, out var component))
                continue;

            if (!TryGetBountyFromId(args.Station, component.Id, out var bounty))
                continue;

            if (!IsBountyComplete(container.Owner, bounty.Value))
                continue;

            TryRemoveBounty(args.Station, bounty.Value);
            FillBountyDatabase(args.Station);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"Bounty \"{bounty.Value.Bounty}\" (id:{bounty.Value.Id}) was fulfilled");
        }
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

    public bool IsBountyComplete(EntityUid container, CargoBountyData data)
    {
        if (!_protoMan.TryIndex<CargoBountyPrototype>(data.Bounty, out var proto))
            return false;

        return IsBountyComplete(container, proto.Entries);
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
        var contained = new HashSet<EntityUid>();
        if (TryComp<ContainerManagerComponent>(container, out var containers))
        {
            foreach (var con in containers.Containers.Values)
            {
                if (con.ID == LabelSystem.ContainerName)
                    continue;

                foreach (var ent in con.ContainedEntities)
                {
                    contained.Add(ent);
                }
            }
        }

        return IsBountyComplete(contained, entries);
    }

    public bool IsBountyComplete(HashSet<EntityUid> entities, IEnumerable<CargoBountyItemEntry> entries)
    {
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
                count++;
                temp.Add(entity);

                if (count >= entry.Amount)
                    break;
            }

            if (count < entry.Amount)
                return false;

            foreach (var ent in temp)
            {
                entities.Remove(ent);
            }
        }

        return true;
    }

    [PublicAPI]
    public bool TryAddBounty(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        // todo: consider making the cargo bounties weighted.
        var bounty = _random.Pick(_protoMan.EnumeratePrototypes<CargoBountyPrototype>().ToList());
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

        var duration = MathF.Round(_random.NextFloat(component.MinBountyTime, component.MaxBountyTime) / 15) * 15;
        var endTime = _timing.CurTime + TimeSpan.FromSeconds(duration);

        component.Bounties.Add(new CargoBountyData(component.TotalBounties, bounty.ID, endTime));
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Added bounty \"{bounty.ID}\" (id:{component.TotalBounties}) to station {ToPrettyString(uid)}");
        component.TotalBounties++;
        return true;
    }

    [PublicAPI]
    public bool TryRemoveBounty(EntityUid uid, int dataId, StationCargoBountyDatabaseComponent? component = null)
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
        int id,
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
        while (query.MoveNext(out var uid, out var bountyDatabase))
        {
            var bounties = new ValueList<CargoBountyData>(bountyDatabase.Bounties);
            foreach (var bounty in bounties)
            {
                if (_timing.CurTime < bounty.EndTime)
                    continue;
                TryRemoveBounty(uid, bounty, bountyDatabase);
                FillBountyDatabase(uid, bountyDatabase);
            }
        }
    }
}
