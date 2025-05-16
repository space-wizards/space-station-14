using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Salvage.JobBoard;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Salvage.JobBoard;

public sealed class SalvageJobBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <summary>
    /// Radio channel that unlock messages are broadcast on.
    /// </summary>
    public static ProtoId<RadioChannelPrototype> UnlockChannel = "Supply";

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

        var oldRank = GetRank(ent);

        ent.Comp.CompletedJobs.Add(job);

        var newRank = GetRank(ent);

        // Add reward
        if (TryComp<StationBankAccountComponent>(ent, out var stationBankAccount))
        {
            _cargo.UpdateBankAccount(
                (ent.Owner, stationBankAccount),
                jobProto.Reward,
                _cargo.CreateAccountDistribution((ent,  stationBankAccount)));
        }

        // We ranked up!
        if (oldRank != newRank)
        {
            // We need to find a computer to send the message from.
            var computerQuery = EntityQueryEnumerator<SalvageJobBoardConsoleComponent>();
            while (computerQuery.MoveNext(out var uid, out _))
            {
                var message = Loc.GetString("job-board-radio-announce", ("rank", FormattedMessage.RemoveMarkupPermissive(Loc.GetString(newRank.Title))));
                _radio.SendRadioMessage(uid, message, UnlockChannel, uid, false);
                break;
            }

            if (newRank.UnlockedMarket is { } market &&
                TryComp<StationCargoOrderDatabaseComponent>(ent, out var stationCargoOrder))
            {
                stationCargoOrder.Markets.Add(market);
            }
        }

        var enumerator = EntityQueryEnumerator<SalvageJobBoardConsoleComponent>();
        while (enumerator.MoveNext(out var consoleUid, out var console))
        {
            UpdateUi((consoleUid, console), ent);
        }

        return true;
    }

    private void OnBUIOpened(Entity<SalvageJobBoardConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not SalvageJobBoardUiKey.Key)
            return;

        if (_station.GetOwningStation(ent.Owner) is not { } station ||
            !TryComp<SalvageJobsDataComponent>(station, out var jobData))
            return;

        UpdateUi(ent, (station, jobData));
    }

    private void UpdateUi(Entity<SalvageJobBoardConsoleComponent> ent, Entity<SalvageJobsDataComponent> stationEnt)
    {
        var jobs = GetAvailableJobs(stationEnt);

        var state = new SalvageJobBoardConsoleState(
            GetRank(stationEnt).Title,
            GetRankProgression(stationEnt),
            jobs.Except(stationEnt.Comp.CompletedJobs).ToList());

        _ui.SetUiState(ent.Owner, SalvageJobBoardUiKey.Key, state);
    }
}
