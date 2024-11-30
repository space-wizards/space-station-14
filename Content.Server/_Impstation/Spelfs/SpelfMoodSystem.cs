using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared._Impstation.Spelfs;
using Content.Shared._Impstation.Spelfs.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared._Impstation.CCVar;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Impstation.Spelfs;

public sealed partial class SpelfMoodsSystem : SharedSpelfMoodSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly UserInterfaceSystem _bui = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public IReadOnlyList<SpelfMood> SharedMoods => _sharedMoods.AsReadOnly();
    private readonly List<SpelfMood> _sharedMoods = new();


    [ValidatePrototypeId<DatasetPrototype>]
    private const string SharedDataset = "SpelfMoodsShared";

    [ValidatePrototypeId<DatasetPrototype>]
    private const string YesAndDataset = "SpelfMoodsYesAnd";

    [ValidatePrototypeId<DatasetPrototype>]
    private const string NoAndDataset = "SpelfMoodsNoAnd";

    [ValidatePrototypeId<DatasetPrototype>]
    private const string WildcardDataset = "SpelfMoodsWildcard";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ActionViewMoods = "ActionViewMoods";

    [ValidatePrototypeId<WeightedRandomPrototype>]
    private const string RandomSpelfMoodDataset = "RandomSpelfMoodDataset";

    public override void Initialize()
    {
        base.Initialize();

        NewSharedMoods();

        SubscribeLocalEvent<SpelfMoodsComponent, ComponentStartup>(OnSpelfMoodInit);
        SubscribeLocalEvent<SpelfMoodsComponent, ComponentShutdown>(OnSpelfMoodShutdown);
        SubscribeLocalEvent<SpelfMoodsComponent, ToggleMoodsScreenEvent>(OnToggleMoodsScreen);
        SubscribeLocalEvent<SpelfMoodsComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<RoundRestartCleanupEvent>((_) => NewSharedMoods());
    }

    private void NewSharedMoods()
    {
        _sharedMoods.Clear();
        for (int i = 0; i < _config.GetCVar(ImpCCVars.SpelfSharedMoodCount); i++)
            TryAddSharedMood();
    }

    public bool TryAddSharedMood(SpelfMood? mood = null, bool checkConflicts = true)
    {
        if (mood == null)
        {
            if (TryPick(SharedDataset, out var moodProto, _sharedMoods))
            {
                mood = RollMood(moodProto);
                checkConflicts = false; // TryPick has cleared this mood already
            }
            else
            {
                return false;
            }
        }

        if (checkConflicts && (GetConflicts(_sharedMoods).Contains(mood.ProtoId) || GetMoodProtoSet(_sharedMoods).Overlaps(mood.Conflicts)))
            return false;

        _sharedMoods.Add(mood);
        var enumerator = EntityManager.EntityQueryEnumerator<SpelfMoodsComponent>();
        while (enumerator.MoveNext(out var ent, out var comp))
        {
            if (!comp.FollowsSharedMoods)
                continue;

            NotifyMoodChange((ent, comp));
        }

        return true;
    }

    private void OnBoundUIOpened(EntityUid uid, SpelfMoodsComponent component, BoundUIOpenedEvent args)
    {
        UpdateBUIState(uid, component);
    }

    private void OnToggleMoodsScreen(EntityUid uid, SpelfMoodsComponent component, ToggleMoodsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _bui.TryToggleUi(uid, SpelfMoodsUiKey.Key, actor.PlayerSession);
    }

    private bool TryPick(string datasetProto, [NotNullWhen(true)] out SpelfMoodPrototype? proto, IEnumerable<SpelfMood>? currentMoods = null, HashSet<string>? conflicts = null)
    {
        var dataset = _proto.Index<DatasetPrototype>(datasetProto);
        var choices = dataset.Values.ToList();

        if (currentMoods == null)
            currentMoods = new HashSet<SpelfMood>();
        if (conflicts == null)
            conflicts = GetConflicts(currentMoods);

        var currentMoodProtos = GetMoodProtoSet(currentMoods);

        while (choices.Count > 0)
        {
            var moodId = _random.PickAndTake(choices);
            if (conflicts.Contains(moodId))
                continue; // Skip proto if an existing mood conflicts with it

            var moodProto = _proto.Index<SpelfMoodPrototype>(moodId);
            if (moodProto.Conflicts.Overlaps(currentMoodProtos))
                continue; // Skip proto if it conflicts with an existing mood

            proto = moodProto;
            return true;
        }

        proto = null;
        return false;
    }

    public void NotifyMoodChange(Entity<SpelfMoodsComponent> ent)
    {
        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        if (ent.Comp.MoodsChangedSound != null)
            _audio.PlayGlobal(ent.Comp.MoodsChangedSound, actor.PlayerSession);

        var msg = Loc.GetString("spelf-moods-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Orange);
    }

    public void UpdateBUIState(EntityUid uid, SpelfMoodsComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var state = new SpelfMoodsBuiState(comp.Moods, comp.FollowsSharedMoods ? _sharedMoods : []);
        _bui.SetUiState(uid, SpelfMoodsUiKey.Key, state);
    }

    public void AddMood(EntityUid uid, SpelfMood mood, SpelfMoodsComponent? comp = null, bool notify = true)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Moods.Add(mood);

        if (notify)
            NotifyMoodChange((uid, comp));

        UpdateBUIState(uid, comp);
    }

    /// <summary>
    /// Creates a SpelfMood instance from the given SpelfMoodPrototype, and rolls
    /// its mood vars.
    /// </summary>
    public SpelfMood RollMood(SpelfMoodPrototype proto)
    {
        var mood = new SpelfMood()
        {
            ProtoId = proto.ID,
            MoodName = proto.MoodName,
            MoodDesc = proto.MoodDesc,
            Conflicts = proto.Conflicts,
        };

        var alreadyChosen = new HashSet<string>();

        foreach (var (name, datasetID) in proto.MoodVarDatasets)
        {
            var dataset = _proto.Index<DatasetPrototype>(datasetID);

            if (proto.AllowDuplicateMoodVars)
            {
                mood.MoodVars.Add(name, _random.Pick(dataset));
                continue;
            }

            var choices = dataset.Values.ToList();
            var foundChoice = false;
            while (choices.Count > 0)
            {
                var choice = _random.PickAndTake(choices);
                if (alreadyChosen.Contains(choice))
                    continue;

                mood.MoodVars.Add(name, choice);
                alreadyChosen.Add(choice);
                foundChoice = true;
                break;
            }

            if (!foundChoice)
            {
                Log.Warning($"Ran out of choices for moodvar \"{name}\" in \"{proto.ID}\"! Picking a duplicate...");
                mood.MoodVars.Add(name, _random.Pick(_proto.Index<DatasetPrototype>(dataset)));
            }
        }

        return mood;
    }

    /// <summary>
    /// Checks if the given mood prototype conflicts with the current moods, and
    /// adds the mood if it does not.
    /// </summary>
    public bool TryAddMood(EntityUid uid, SpelfMoodPrototype moodProto, SpelfMoodsComponent? comp = null, bool allowConflict = false, bool notify = true)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (!allowConflict && GetConflicts(uid, comp).Contains(moodProto.ID))
            return false;

        AddMood(uid, RollMood(moodProto), comp, notify);
        return true;
    }

    public bool TryAddRandomMood(EntityUid uid, string datasetProto, SpelfMoodsComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (TryPick(datasetProto, out var moodProto, GetActiveMoods(uid, comp)))
        {
            AddMood(uid, RollMood(moodProto), comp);
            return true;
        }

        return false;
    }

    public bool TryAddRandomMood(EntityUid uid, SpelfMoodsComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        var datasetProto = _proto.Index<WeightedRandomPrototype>(RandomSpelfMoodDataset).Pick();

        return TryAddRandomMood(uid, datasetProto, comp);
    }

    public void SetMoods(EntityUid uid, IEnumerable<SpelfMood> moods, SpelfMoodsComponent? comp = null, bool notify = true)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Moods = moods.ToList();
        if (notify)
            NotifyMoodChange((uid, comp));

        UpdateBUIState(uid, comp);
    }

    public HashSet<string> GetConflicts(IEnumerable<SpelfMood> moods)
    {
        var conflicts = new HashSet<string>();

        foreach (var mood in moods)
        {
            conflicts.Add(mood.ProtoId); // Specific moods shouldn't be added twice
            conflicts.UnionWith(mood.Conflicts);
        }

        return conflicts;
    }

    public HashSet<string> GetConflicts(EntityUid uid, SpelfMoodsComponent? moods = null)
    {
        // TODO: Should probably cache this when moods get updated

        if (!Resolve(uid, ref moods))
            return new();

        var conflicts = GetConflicts(GetActiveMoods(uid, moods));

        return conflicts;
    }

    public HashSet<string> GetMoodProtoSet(IEnumerable<SpelfMood> moods)
    {
        var moodProtos = new HashSet<string>();
        foreach (var mood in moods)
            if (!string.IsNullOrEmpty(mood.ProtoId))
                moodProtos.Add(mood.ProtoId);
        return moodProtos;
    }

    /// <summary>
    /// Return a list of the moods that are affecting this entity.
    /// </summary>
    public List<SpelfMood> GetActiveMoods(EntityUid uid, SpelfMoodsComponent? comp = null, bool includeShared = true)
    {
        if (!Resolve(uid, ref comp))
            return [];

        if (includeShared && comp.FollowsSharedMoods)
        {
            return new List<SpelfMood>(SharedMoods.Concat(comp.Moods));
        }
        else
        {
            return comp.Moods;
        }
    }

    private void OnSpelfMoodInit(EntityUid uid, SpelfMoodsComponent comp, ComponentStartup args)
    {
        if (comp.LifeStage != ComponentLifeStage.Starting)
            return;

        // "Yes, and" moods
        if (TryPick(YesAndDataset, out var mood, GetActiveMoods(uid, comp)))
            TryAddMood(uid, mood, comp, true, false);

        // "No, and" moods
        if (TryPick(NoAndDataset, out mood, GetActiveMoods(uid, comp)))
            TryAddMood(uid, mood, comp, true, false);

        comp.Action = _actions.AddAction(uid, ActionViewMoods);
    }

    private void OnSpelfMoodShutdown(EntityUid uid, SpelfMoodsComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.Action);
    }

    protected override void OnEmagged(EntityUid uid, SpelfMoodsComponent comp, ref GotEmaggedEvent args)
    {
        base.OnEmagged(uid, comp, ref args);
        TryAddRandomMood(uid, WildcardDataset, comp);
    }
}
