using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Salvage.JobBoard;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage.JobBoard;

public sealed class SalvageJobBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
        SubscribeLocalEvent<SalvageJobBoardConsoleComponent, BoundUIOpenedEvent>(OnBUIOpened);
    }

    private void OnSold(ref EntitySoldEvent args)
    {
        if (!TryComp<SalvageJobsDataComponent>(args.Station, out var salvageJobsData))
            return;

        foreach (var sold in args.Sold)
        {
            if (!_label.TryGetLabel<JobBoardLabelComponent>(sold, out var labelEnt))
                continue;

            var jobId = labelEnt.Value.Comp.JobId;

            if (!_cargo.IsBountyComplete(sold, jobId))
                continue;

            TryCompleteSalvageJob((args.Station, salvageJobsData), jobId);
        }
    }

    public List<ProtoId<CargoBountyPrototype>> GetAvailableJobs(Entity<SalvageJobsDataComponent> ent)
    {
        var outJobs = new List<ProtoId<CargoBountyPrototype>>();
        var availableGroups = new HashSet<ProtoId<CargoBountyGroupPrototype>>();

        var completedCount = ent.Comp.CompletedJobs.Count;
        foreach (var (thresholds, rank) in ent.Comp.RankThresholds)
        {
            if (completedCount < thresholds)
                continue;
            if (rank.BountyGroup == null)
                continue;
            availableGroups.Add(rank.BountyGroup.Value);
        }

        foreach (var bounty in _prototypeManager.EnumeratePrototypes<CargoBountyPrototype>())
        {
            if (availableGroups.Contains(bounty.Group))
                outJobs.Add(bounty);
        }

        return outJobs;
    }

    public float GetRankProgression(Entity<SalvageJobsDataComponent> ent)
    {
        // Need to have at least two of these.
        if (ent.Comp.RankThresholds.Count <= 1)
            return 1;
        var completedCount = ent.Comp.CompletedJobs.Count;

        foreach (var (low, rank) in ent.Comp.RankThresholds.Reverse())
        {
            if (completedCount < low)
                continue;
            var totalCount = _prototypeManager.EnumeratePrototypes<CargoBountyPrototype>()
                .Count(p => p.Group == rank.BountyGroup);

            if (totalCount == 0)
                return 1;

            return (completedCount - low) / (float) totalCount;
        }

        return 1f;
    }

    public bool IsMaxRank(Entity<SalvageJobsDataComponent> ent)
    {
        return GetAvailableJobs(ent).Count == ent.Comp.CompletedJobs.Count;
    }

    public SalvageRankDatum GetRank(Entity<SalvageJobsDataComponent> ent)
    {
        if (IsMaxRank(ent))
            return ent.Comp.MaxRank;
        var completedCount = ent.Comp.CompletedJobs.Count;

        foreach (var (threshold, rank) in ent.Comp.RankThresholds.Reverse())
        {
            if (completedCount < threshold)
                continue;

            return rank;
        }
        // base case
        return ent.Comp.RankThresholds[0];
    }

    public bool TryCompleteSalvageJob(Entity<SalvageJobsDataComponent> ent, ProtoId<CargoBountyPrototype> job)
    {
        if (ent.Comp.CompletedJobs.Contains(job) || !GetAvailableJobs(ent).Contains(job))
            return false;

        var jobProto = _prototypeManager.Index(job);

        ent.Comp.CompletedJobs.Add(job);

        // Add reward
        if (TryComp<StationBankAccountComponent>(ent, out var stationBankAccount))
        {
            _cargo.UpdateBankAccount(
                (ent.Owner, stationBankAccount),
                jobProto.Reward,
                _cargo.CreateAccountDistribution((ent,  stationBankAccount)));
        }

        // TODO: implement unlocking cargo orders

        return true;
    }

    private void OnBUIOpened(Entity<SalvageJobBoardConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not SalvageJobBoardUiKey.Key)
            return;

        if (_station.GetOwningStation(ent.Owner) is not { } station ||
            !TryComp<SalvageJobsDataComponent>(station, out var jobData))
            return;

        var stationEnt = (Entity<SalvageJobsDataComponent>) (station, jobData);
        var jobs = GetAvailableJobs(stationEnt);

        var state = new SalvageJobBoardConsoleState(
            GetRank(stationEnt).Title,
            GetRankProgression(stationEnt),
            jobs.Except(stationEnt.Comp.CompletedJobs).ToList());

        _ui.SetUiState(ent.Owner, SalvageJobBoardUiKey.Key, state);
    }
}
