using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Labels;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;

    private void InitializeBounty()
    {
        SubscribeLocalEvent<CargoBountyLabelComponent, PriceCalculationEvent>(OnGetBountyPrice);
        SubscribeLocalEvent<CargoBountyLabelComponent, EntitySoldEvent>(OnSold);

        SubscribeLocalEvent<StationCargoBountyDatabaseComponent, MapInitEvent>(OnMapInit);
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

        if (!IsBountyComplete(container.Owner, bounty.Value.Bounty.Entries))
            return;
        args.Handled = true;

        // Make the bounty itself cost nothing.
        component.Calculating = true;
        args.Price -= _pricing.GetPrice(container.Owner);
        component.Calculating = false;
        args.Price += bounty.Value.Bounty.Reward;
    }

    private void OnSold(EntityUid uid, CargoBountyLabelComponent component, ref EntitySoldEvent args)
    {
        // make sure this label was actually applied to a crate.
        if (!_container.TryGetContainingContainer(uid, out var container) || container.ID != LabelSystem.ContainerName)
            return;

        if (!TryGetBountyFromId(args.Station, component.Id, out var bounty))
            return;

        if (!IsBountyComplete(container.Owner, bounty.Value.Bounty.Entries))
            return;

        TryRemoveBounty(args.Station, bounty.Value);
        FillBountyDatabase(args.Station);
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
                if (!entry.Whitelist.IsValid(entity))
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
        var bounty = _random.Pick(_prototypeManager.EnumeratePrototypes<CargoBountyPrototype>().ToList());
        return TryAddBounty(uid, bounty, component);
    }

    [PublicAPI]
    public bool TryAddBounty(EntityUid uid, string bountyId, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!_prototypeManager.TryIndex<CargoBountyPrototype>(bountyId, out var bounty))
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

        var endTime = _timing.CurTime + _random.Pick(component.BountyDurations);
        component.Bounties.Add(new CargoBountyData(component.TotalBounties, bounty, endTime));
        component.TotalBounties++;
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Added bounty {bounty.ID} to station {ToPrettyString(uid)}");
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

        foreach (var bounty in new List<CargoBountyData>(component.Bounties))
        {
            if (bounty.BountyId == data.BountyId)
            {
                component.Bounties.Remove(data);
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
            if (bountyData.BountyId != id)
                continue;
            bounty = bountyData;
            break;
        }

        return bounty != null;
    }

    private void UpdateBounty()
    {
        var query = EntityQueryEnumerator<StationCargoBountyDatabaseComponent>();
        while (query.MoveNext(out var uid, out var bountyDatabase))
        {
            var bounties = new List<CargoBountyData>(bountyDatabase.Bounties);
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
