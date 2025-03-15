using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared._Impstation.Thaven;
using Content.Shared._Impstation.Thaven.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared._Impstation.CCVar;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Impstation.Thaven;

public sealed partial class ThavenMoodsSystem : SharedThavenMoodSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly UserInterfaceSystem _bui = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public IReadOnlyList<ThavenMood> SharedMoods => _sharedMoods.AsReadOnly();
    private readonly List<ThavenMood> _sharedMoods = new();
    // cached hashset that never gets modified
    private readonly HashSet<ThavenMood> _emptyMoods = new HashSet<ThavenMood>();
    // cached hashset that gets changed in GetMoodProtoSet
    private readonly HashSet<ProtoId<ThavenMoodPrototype>> _moodProtos = new HashSet<ProtoId<ThavenMoodPrototype>>();

    private ProtoId<DatasetPrototype> SharedDataset = "ThavenMoodsShared";
    private ProtoId<DatasetPrototype> YesAndDataset = "ThavenMoodsYesAnd";
    private ProtoId<DatasetPrototype> NoAndDataset = "ThavenMoodsNoAnd";
    private ProtoId<DatasetPrototype> WildcardDataset = "ThavenMoodsWildcard";

    private EntProtoId ActionViewMoods = "ActionViewMoods";

    private ProtoId<WeightedRandomPrototype> RandomThavenMoodDataset = "RandomThavenMoodDataset";

    public override void Initialize()
    {
        base.Initialize();

        NewSharedMoods();

        SubscribeLocalEvent<ThavenMoodsComponent, MapInitEvent>(OnThavenMoodInit);
        SubscribeLocalEvent<ThavenMoodsComponent, ComponentShutdown>(OnThavenMoodShutdown);
        SubscribeLocalEvent<ThavenMoodsComponent, ToggleMoodsScreenEvent>(OnToggleMoodsScreen);
        SubscribeLocalEvent<ThavenMoodsComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<RoundRestartCleanupEvent>((_) => NewSharedMoods());
    }

    private void NewSharedMoods()
    {
        _sharedMoods.Clear();
        for (int i = 0; i < _config.GetCVar(ImpCCVars.ThavenSharedMoodCount); i++)
            TryAddSharedMood(notify: false); // don't spam notify if there are multiple moods

        NotifySharedMoodChange();
    }

    public bool TryAddSharedMood(ThavenMood? mood = null, bool checkConflicts = true, bool notify = true)
    {
        if (mood == null)
        {
            if (!TryPick(SharedDataset, out var moodProto, _sharedMoods))
                return false;

            mood = RollMood(moodProto);
            checkConflicts = false; // TryPick has cleared this mood already
        }

        if (checkConflicts && SharedMoodConflicts(mood))
            return false;

        _sharedMoods.Add(mood);

        if (notify)
            NotifySharedMoodChange();

        return true;
    }

    private bool SharedMoodConflicts(ThavenMood mood)
    {
        return mood.ProtoId is {} id &&
            (GetConflicts(_sharedMoods).Contains(id) ||
            GetMoodProtoSet(_sharedMoods).Overlaps(mood.Conflicts));
    }

    private void NotifySharedMoodChange()
    {
        var query = EntityQueryEnumerator<ThavenMoodsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.FollowsSharedMoods)
                continue;

            NotifyMoodChange((uid, comp));
        }
    }

    private void OnBoundUIOpened(Entity<ThavenMoodsComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateBUIState(ent);
    }

    private void OnToggleMoodsScreen(Entity<ThavenMoodsComponent> ent, ref ToggleMoodsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;

        _bui.TryToggleUi(ent.Owner, ThavenMoodsUiKey.Key, actor.PlayerSession);
    }

    private bool TryPick(string datasetProto, [NotNullWhen(true)] out ThavenMoodPrototype? proto, IEnumerable<ThavenMood>? currentMoods = null, HashSet<ProtoId<ThavenMoodPrototype>>? conflicts = null)
    {
        var dataset = _proto.Index<DatasetPrototype>(datasetProto);
        var choices = dataset.Values.ToList();

        currentMoods ??= _emptyMoods;
        conflicts ??= GetConflicts(currentMoods);

        var currentMoodProtos = GetMoodProtoSet(currentMoods);

        while (choices.Count > 0)
        {
            var moodId = _random.PickAndTake(choices);
            if (conflicts.Contains(moodId))
                continue; // Skip proto if an existing mood conflicts with it

            var moodProto = _proto.Index<ThavenMoodPrototype>(moodId);
            if (moodProto.Conflicts.Overlaps(currentMoodProtos))
                continue; // Skip proto if it conflicts with an existing mood

            proto = moodProto;
            return true;
        }

        proto = null;
        return false;
    }

    /// <summary>
    /// Send the player a audiovisual notification and update the moods UI.
    /// </summary>
    public void NotifyMoodChange(Entity<ThavenMoodsComponent> ent)
    {
        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var session = actor.PlayerSession;
        _audio.PlayGlobal(ent.Comp.MoodsChangedSound, session);

        var msg = Loc.GetString("thaven-moods-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, session.Channel, colorOverride: Color.Orange);

        // update the UI without needing to re-open it
        UpdateBUIState(ent);
    }

    public void UpdateBUIState(Entity<ThavenMoodsComponent> ent)
    {
        var state = new ThavenMoodsBuiState(ent.Comp.FollowsSharedMoods ? _sharedMoods : []);
        _bui.SetUiState(ent.Owner, ThavenMoodsUiKey.Key, state);
    }

    /// <summary>
    /// Directly add a mood to a thaven, ignoring conflicts.
    /// </summary>
    public void AddMood(Entity<ThavenMoodsComponent> ent, ThavenMood mood, bool notify = true)
    {
        ent.Comp.Moods.Add(mood);
        Dirty(ent);

        if (notify)
            NotifyMoodChange(ent);
        else // NotifyMoodChange will update UI so this is in else
            UpdateBUIState(ent);
    }

    /// <summary>
    /// Creates a ThavenMood instance from the given ThavenMoodPrototype, and rolls
    /// its mood vars.
    /// </summary>
    public ThavenMood RollMood(ThavenMoodPrototype proto)
    {
        var mood = proto.ShallowClone();
        var alreadyChosen = new HashSet<ProtoId<ThavenMoodPrototype>>();

        foreach (var (name, datasetID) in proto.MoodVarDatasets)
        {
            var dataset = _proto.Index(datasetID);

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
                mood.MoodVars.Add(name, _random.Pick(dataset));
            }
        }

        return mood;
    }

    /// <summary>
    /// Checks if the given mood prototype conflicts with the current moods, and
    /// adds the mood if it does not.
    /// </summary>
    public bool TryAddMood(Entity<ThavenMoodsComponent> ent, ThavenMoodPrototype moodProto, bool allowConflict = false, bool notify = true)
    {
        if (!allowConflict && GetConflicts(ent).Contains(moodProto.ID))
            return false;

        AddMood(ent, RollMood(moodProto), notify);
        return true;
    }

    /// <summary>
    /// Tries to add a random mood using a specific dataset.
    /// </summary>
    public bool TryAddRandomMood(Entity<ThavenMoodsComponent> ent, string datasetProto, bool notify = true)
    {
        if (TryPick(datasetProto, out var moodProto, GetActiveMoods(ent)))
        {
            AddMood(ent, RollMood(moodProto), notify);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to add a random mood using <see cref="RandomThavenMoodDataset"/>.
    /// </summary>
    public bool TryAddRandomMood(Entity<ThavenMoodsComponent> ent, bool notify = true)
    {
        var datasetProto = _proto.Index(RandomThavenMoodDataset).Pick();
        return TryAddRandomMood(ent, datasetProto, notify);
    }

    /// <summary>
    /// Tries to add a random mood from <see cref="WildcardDataset"/>, which is the same as emagging.
    /// </summary>
    public bool AddWildcardMood(Entity<ThavenMoodsComponent> ent, bool notify = true)
    {
        return TryAddRandomMood(ent, WildcardDataset, notify);
    }

    /// <summary>
    /// Set the moods for a thaven directly.
    /// This does NOT check conflicts so be careful with what you set!
    /// </summary>
    public void SetMoods(Entity<ThavenMoodsComponent> ent, IEnumerable<ThavenMood> moods, bool notify = true)
    {
        ent.Comp.Moods = moods.ToList();
        Dirty(ent);

        if (notify)
            NotifyMoodChange(ent);
        else
            UpdateBUIState(ent);
    }

    /// <summary>
    /// lazily wipes all of a Thaven's moods. This leaves them mood-less.
    /// </summary>
    public void ClearMoods(Entity<ThavenMoodsComponent> ent, bool notify = false)
    {
        ent.Comp.Moods = new List<ThavenMood>();
        Dirty(ent);

        if (notify)
            NotifyMoodChange(ent);
        else
            UpdateBUIState(ent);
    }

    /// <summary>
    /// Allows external systems to toggle wether or not a ThavenMoodsComponent follows the shared thaven mood.
    /// </summary>
    public void ToggleSharedMoods(Entity<ThavenMoodsComponent> ent, bool notify = false)
    {
        if (!ent.Comp.FollowsSharedMoods)
            ent.Comp.FollowsSharedMoods = true;
        else
            ent.Comp.FollowsSharedMoods = false;
        Dirty(ent);

        if (notify)
            NotifyMoodChange(ent);
        else
            UpdateBUIState(ent);
    }

    /// <summary>
    /// Allows external sytems to toggle wether or not a ThavenMoodsComponent is emaggable.
    /// </summary>
    public void ToggleEmaggable(Entity<ThavenMoodsComponent> ent)
    {
        if (!ent.Comp.CanBeEmagged)
            ent.Comp.CanBeEmagged = true;
        else
            ent.Comp.CanBeEmagged = false;
        Dirty(ent);
    }

    public HashSet<ProtoId<ThavenMoodPrototype>> GetConflicts(IEnumerable<ThavenMood> moods)
    {
        var conflicts = new HashSet<ProtoId<ThavenMoodPrototype>>();

        foreach (var mood in moods)
        {
            if (mood.ProtoId is {} id)
                conflicts.Add(id); // Specific moods shouldn't be added twice
            conflicts.UnionWith(mood.Conflicts);
        }

        return conflicts;
    }

    /// <summary>
    /// Get the conflicts for a thaven's active moods.
    /// </summary>
    public HashSet<ProtoId<ThavenMoodPrototype>> GetConflicts(Entity<ThavenMoodsComponent> ent)
    {
        // TODO: Should probably cache this when moods get updated
        return GetConflicts(GetActiveMoods(ent));
    }

    /// <summary>
    /// Maps some moods to their ids.
    /// The hashset returned is reused and so you must not modify it.
    /// </summary>
    public HashSet<ProtoId<ThavenMoodPrototype>> GetMoodProtoSet(IEnumerable<ThavenMood> moods)
    {
        _moodProtos.Clear();
        foreach (var mood in moods)
        {
            if (mood.ProtoId is {} id)
                _moodProtos.Add(id);
        }

        return _moodProtos;
    }

    /// <summary>
    /// Return a list of the moods that are affecting this entity.
    /// </summary>
    public List<ThavenMood> GetActiveMoods(Entity<ThavenMoodsComponent> ent, bool includeShared = true)
    {
        if (includeShared && ent.Comp.FollowsSharedMoods)
            return new List<ThavenMood>(SharedMoods.Concat(ent.Comp.Moods));

        return ent.Comp.Moods;
    }

    private void OnThavenMoodInit(Entity<ThavenMoodsComponent> ent, ref MapInitEvent args)
    {
        // "Yes, and" moods
        if (TryPick(YesAndDataset, out var mood, GetActiveMoods(ent)))
            TryAddMood(ent, mood, true, false);

        // "No, and" moods
        if (TryPick(NoAndDataset, out mood, GetActiveMoods(ent)))
            TryAddMood(ent, mood, true, false);

        ent.Comp.Action = _actions.AddAction(ent.Owner, ActionViewMoods);
    }

    private void OnThavenMoodShutdown(Entity<ThavenMoodsComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent, ent.Comp.Action);
    }

    protected override void OnEmagged(Entity<ThavenMoodsComponent> ent, ref GotEmaggedEvent args)
    {
        base.OnEmagged(ent, ref args);
        if (!args.Handled)
            return;

        AddWildcardMood(ent);
    }
}
