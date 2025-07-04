using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Radio;
using Content.Shared.Salvage.JobBoard;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Salvage.JobBoard;

public sealed class SalvageJobBoardSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <summary>
    /// Radio channel that unlock messages are broadcast on.
    /// </summary>
    private static readonly ProtoId<RadioChannelPrototype> UnlockChannel = "Supply";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
        SubscribeLocalEvent<SalvageJobBoardConsoleComponent, BoundUIOpenedEvent>(OnBUIOpened);
        Subs.BuiEvents<SalvageJobBoardConsoleComponent>(SalvageJobBoardUiKey.Key,
            subs =>
            {
                subs.Event<JobBoardPrintLabelMessage>(OnPrintLabelMessage);
            });
    }

    private void OnSold(ref EntitySoldEvent args)
    {
        if (!TryComp<SalvageJobsDataComponent>(args.Station, out var salvageJobsData))
            return;

        foreach (var sold in args.Sold)
        {
            if (!FulfillsSalvageJob(sold, (args.Station, salvageJobsData), out var jobId))
                return;
            TryCompleteSalvageJob((args.Station, salvageJobsData), jobId.Value);
        }
    }

    /// <summary>
    /// Gets the jobs that the station can currently access.
    /// </summary>
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
            if (ent.Comp.CompletedJobs.Contains(bounty))
                continue;

            if (availableGroups.Contains(bounty.Group))
                outJobs.Add(bounty);
        }

        return outJobs;
    }

    /// <summary>
    /// Gets the "progression" of a rank, expressed as on the range [0, 1]
    /// </summary>
    public float GetRankProgression(Entity<SalvageJobsDataComponent> ent)
    {
        // Need to have at least two of these.
        if (ent.Comp.RankThresholds.Count <= 1)
            return 1;
        var completedCount = ent.Comp.CompletedJobs.Count;

        for (var i = ent.Comp.RankThresholds.Count - 1; i >= 0; i--)
        {
            var low = ent.Comp.RankThresholds.Keys.ElementAt(i);

            if (completedCount < low)
                continue;

            // don't worry abooouuuuut it (it'll be O K !)
            var high = i != ent.Comp.RankThresholds.Count - 1
                ? ent.Comp.RankThresholds.Keys.ElementAt(i + 1)
                :  _prototypeManager.EnumeratePrototypes<CargoBountyPrototype>()
                .Count(p => ent.Comp.RankThresholds.Values
                    .Select(r => r.BountyGroup)
                    .Contains(p.Group));

            return (completedCount - low) / (float)(high - low);
        }

        return 1f;
    }

    /// <summary>
    /// Checks if the current station is the max rank
    /// </summary>
    public bool IsMaxRank(Entity<SalvageJobsDataComponent> ent)
    {
        return GetAvailableJobs(ent).Count == 0;
    }

    /// <summary>
    /// Gets the current rank of the station
    /// </summary>
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="job"></param>
    /// <returns></returns>
    public bool TryCompleteSalvageJob(Entity<SalvageJobsDataComponent> ent, ProtoId<CargoBountyPrototype> job)
    {
        if (!GetAvailableJobs(ent).Contains(job))
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

    /// <summary>
    /// Checks if a given entity fulfills a bounty for the station.
    /// </summary>
    public bool FulfillsSalvageJob(EntityUid uid, Entity<SalvageJobsDataComponent>? station, [NotNullWhen(true)] out ProtoId<CargoBountyPrototype>? job)
    {
        job = null;

        if (!_label.TryGetLabel<JobBoardLabelComponent>(uid, out var labelEnt))
            return false;

        if (labelEnt.Value.Comp.JobId is not { } jobId)
            return false;

        job = jobId;

        if (station is null)
        {
            if (_station.GetOwningStation(uid) is not { } stationUid ||
                !TryComp<SalvageJobsDataComponent>(stationUid, out var stationComp))
                return false;

            station = (stationUid, stationComp);
        }

        if (!GetAvailableJobs((station.Value, station.Value.Comp)).Contains(job.Value))
            return false;


        if (!_cargo.IsBountyComplete(uid, job))
            return false;

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

    private void OnPrintLabelMessage(Entity<SalvageJobBoardConsoleComponent> ent, ref JobBoardPrintLabelMessage args)
    {
        if (_timing.CurTime < ent.Comp.NextPrintTime)
            return;

        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<SalvageJobsDataComponent>(station, out var jobsData))
            return;

        if (!_prototypeManager.TryIndex<CargoBountyPrototype>(args.JobId, out var job))
            return;

        if (!GetAvailableJobs((station, jobsData)).Contains(args.JobId))
            return;

        _audio.PlayPvs(ent.Comp.PrintSound, ent);
        var label = SpawnAtPosition(ent.Comp.LabelEntity, Transform(ent).Coordinates);
        EnsureComp<JobBoardLabelComponent>(label).JobId = job.ID;

        var target = new List<string>();
        foreach (var entry in job.Entries)
        {
            target.Add(Loc.GetString("bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name))));
        }
        _paper.SetContent(label, Loc.GetString("job-board-label-text", ("target", string.Join(',', target)), ("reward", job.Reward)));

        ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintDelay;
    }

    private void UpdateUi(Entity<SalvageJobBoardConsoleComponent> ent, Entity<SalvageJobsDataComponent> stationEnt)
    {
        var state = new SalvageJobBoardConsoleState(
            GetRank(stationEnt).Title,
            GetRankProgression(stationEnt),
            GetAvailableJobs(stationEnt));

        _ui.SetUiState(ent.Owner, SalvageJobBoardUiKey.Key, state);
    }
}
