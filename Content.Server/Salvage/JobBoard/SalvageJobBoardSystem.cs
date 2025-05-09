using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Salvage.JobBoard;

public sealed class SalvageJobBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
    }

    private void OnSold(ref EntitySoldEvent args)
    {
        foreach (var sold in args.Sold)
        {

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

        var high = ent.Comp.RankThresholds.Keys.First();
        int low;
        for (var i = 1; i < ent.Comp.RankThresholds.Count; i++)
        {
            low = high;
            high = ent.Comp.RankThresholds.Keys.ElementAt(i);

            if (completedCount >= high)
                continue;
            return Math.Clamp(MathHelper.Lerp(low, high, completedCount), 0, 1);
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
}
