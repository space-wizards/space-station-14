using System.Linq;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.ReadyManifest;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;

    private readonly Dictionary<ICommonSession, ReadyManifestEui> _openEuis = [];
    private Dictionary<ProtoId<JobPrototype>, int> _jobCounts = [];

    public override void Initialize()
    {
        SubscribeNetworkEvent<RequestReadyManifestMessage>(OnRequestReadyManifest);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<PlayerToggleReadyEvent>(OnPlayerToggleReady);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        foreach (var (_, eui) in _openEuis)
        {
            eui.Close();
        }

        _openEuis.Clear();
    }

    private void OnRequestReadyManifest(RequestReadyManifestMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } sessionCast)
        {
            return;
        }
        BuildReadyManifest();
        OpenEui(sessionCast);
    }

    private void OnPlayerToggleReady(PlayerToggleReadyEvent ev)
    {
        var userId = ev.PlayerSession.Data.UserId;

        if (!_prefsManager.TryGetCachedPreferences(userId, out var preferences))
        {
            return;
        }

        var profile = (HumanoidCharacterProfile)preferences.SelectedCharacter;
        var profileJobs = FilterPlayerJobs(profile);

        if (_gameTicker.PlayerGameStatuses[userId] == PlayerGameStatus.ReadyToPlay)
        {
            foreach (var job in profileJobs)
            {
                _jobCounts.TryGetValue(job, out var value);
                _jobCounts[job] = ++value;
            }
        }
        else
        {
            foreach (var job in profileJobs)
            {
                if (_jobCounts.TryGetValue(job, out var value))
                {
                    _jobCounts[job] = --value;
                }
            }
        }

        UpdateEuis();
    }

    private void BuildReadyManifest()
    {
        var jobCounts = new Dictionary<ProtoId<JobPrototype>, int>();

        foreach (var (userId, status) in _gameTicker.PlayerGameStatuses)
        {
            if (status == PlayerGameStatus.ReadyToPlay)
            {
                HumanoidCharacterProfile profile;
                if (_prefsManager.TryGetCachedPreferences(userId, out var preferences))
                {
                    profile = (HumanoidCharacterProfile)preferences.SelectedCharacter;
                    var profileJobs = FilterPlayerJobs(profile);
                    foreach (var jobId in profileJobs)
                    {
                        jobCounts.TryGetValue(jobId, out var value);
                        jobCounts[jobId] = ++value;
                    }
                }
            }
        }
        _jobCounts = jobCounts;
    }

    private List<ProtoId<JobPrototype>> FilterPlayerJobs(HumanoidCharacterProfile profile)
    {
        var jobs = profile.JobPriorities.Keys.Select(k => new ProtoId<JobPrototype>(k)).ToList();
        List<ProtoId<JobPrototype>> priorityJobs = [];
        foreach (var job in jobs)
        {
            var priority = profile.JobPriorities[job];
            // For jobs that are rolled before others, such as Command, we want to check for any priority since they'll always be filled
            if (priority == JobPriority.High || _prototypeManager.Index(job).Weight >= 10 && priority > JobPriority.Never)
            {
                priorityJobs.Add(job);
            }
        }
        return priorityJobs;
    }

    public Dictionary<ProtoId<JobPrototype>, int> GetReadyManifest()
    {
        return _jobCounts;
    }

    public void OpenEui(ICommonSession session)
    {
        if (_openEuis.ContainsKey(session))
        {
            return;
        }

        var eui = new ReadyManifestEui(this);
        _openEuis.Add(session, eui);
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    private void UpdateEuis()
    {
        foreach (var (_, eui) in _openEuis)
        {
            eui.StateDirty();
        }
    }

    public void CloseEui(ICommonSession session)
    {
        if (!_openEuis.TryGetValue(session, out var eui))
        {
            return;
        }

        _openEuis.Remove(session);
        eui.Close();
    }
}
