using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Dataset;
using Content.Shared.Impstation.Spelfs;
using Content.Shared.Impstation.Spelfs.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Impstation.Spelfs;

public sealed partial class SpelfMoodSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    [ValidatePrototypeId<DatasetPrototype>]
    private const string SharedDataset = "SpelfMoodsShared";

    [ValidatePrototypeId<DatasetPrototype>]
    private const string YesAndDataset = "SpelfMoodsYesAnd";

    [ValidatePrototypeId<DatasetPrototype>]
    private const string NoAndDataset = "SpelfMoodsNoAnd";

    [ValidatePrototypeId<DatasetPrototype>]
    private const string WildcardDataset = "SpelfMoodsWildcard";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpelfMoodComponent, ComponentStartup>(OnSpelfMoodInit);
    }

    private bool TryPick(string datasetProto, HashSet<string> conflicts, HashSet<string> currentMoods, [NotNullWhen(true)] out SpelfMoodPrototype? proto)
    {
        var dataset = _proto.Index<DatasetPrototype>(datasetProto);
        var choices = dataset.Values.ToList();

        while (choices.Count > 0)
        {
            var moodId = _random.PickAndTake(choices);
            if (conflicts.Contains(moodId))
                continue; // Skip proto if an existing mood conflicts with it

            var moodProto = _proto.Index<SpelfMoodPrototype>(moodId);
            if (moodProto.Conflicts.Overlaps(currentMoods))
                continue; // Skip proto if it conflicts with an existing mood

            proto = moodProto;
            return true;
        }

        proto = null;
        return false;
    }

    public void NotifyMoodChange(EntityUid uid)
    {
        if (!HasComp<ActorComponent>(uid))
            return;

        // TODO: Copy NotifyLawsChanged
    }

    public void AddMood(EntityUid uid, SpelfMood mood, SpelfMoodComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Moods.Add(mood);
        NotifyMoodChange(uid);
    }

    /// <summary>
    /// Creates a SpelfMood instance from the given SpelfMoodPrototype, and rolls
    /// its mood vars.
    /// </summary>
    public SpelfMood RollMood(SpelfMoodPrototype proto)
    {
        var mood = new SpelfMood();
        mood.ProtoId = proto.ID;
        mood.MoodString = proto.MoodString;

        foreach (var (name, dataset) in proto.MoodVarDatasets)
            mood.MoodVars.Add(name, _random.Pick(_proto.Index<DatasetPrototype>(dataset)));

        return mood;
    }

    /// <summary>
    /// Checks if the given mood prototype conflicts with the current moods, and
    /// adds the mood if it does not.
    /// </summary>
    public bool TryAddMood(EntityUid uid, SpelfMoodPrototype moodProto, SpelfMoodComponent? comp = null, bool allowConflict = false)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (!allowConflict && comp.Conflicts.Contains(moodProto.ID))
            return false;

        comp.Conflicts.UnionWith(moodProto.Conflicts);

        AddMood(uid, RollMood(moodProto), comp);
        return true;
    }

    public void OnSpelfMoodInit(EntityUid uid, SpelfMoodComponent comp, ComponentStartup args)
    {
        if (comp.LifeStage != ComponentLifeStage.Starting)
            return;

        // Shared moods
        // TODO MAKE THIS ACTUALLY SHARED
        if (TryPick(SharedDataset, comp.Conflicts, comp.MoodProtoSet(), out var mood))
            TryAddMood(uid, mood, comp, true);

        // "Yes, and" moods
        if (TryPick(YesAndDataset, comp.Conflicts, comp.MoodProtoSet(), out mood))
            TryAddMood(uid, mood, comp, true);

        // "No, and" moods
        if (TryPick(NoAndDataset, comp.Conflicts, comp.MoodProtoSet(), out mood))
            TryAddMood(uid, mood, comp, true);

        // Wildcard moods
        if (TryPick(WildcardDataset, comp.Conflicts, comp.MoodProtoSet(), out mood))
            TryAddMood(uid, mood, comp, true);

    }
}
