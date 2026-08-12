using Content.Server.Chat.Managers;
using Content.Server.Mapping;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

/// <summary>
/// Handles automatically saving the full state of the game.
/// </summary>
/// <remarks>
/// This handles map-init saving of the whole game, pre-init mapping is handled by <see cref="MappingSystem"/>
/// </remarks>
public sealed partial class GameAutoSavesSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IChatManager _chat = default!;

    // All data below doesn't need to be saved because, well... It's an autosave system.

    private TimeSpan _nextAutosave = TimeSpan.Zero;

    private bool _enabled;
    private bool _firstMessageSent;
    private bool _secondMessageSent;
    private TimeSpan _interval = TimeSpan.Zero;
    private TimeSpan _intervalMessage1 = TimeSpan.Zero;
    private TimeSpan _intervalMessage2 = TimeSpan.Zero;
    private ResPath _directory = ResPath.Empty;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, CCVars.AutoSavesInterval, OnIntervalChanged, true);
        Subs.CVar(_config, CCVars.AutoSavesMessageIntervalFirst, i => _intervalMessage1 = TimeSpan.FromMinutes(i), true);
        Subs.CVar(_config, CCVars.AutoSavesMessageIntervalSecond, i => _intervalMessage2 = TimeSpan.FromMinutes(i), true);
        Subs.CVar(_config, CCVars.AutoSavesEnabled, b => _enabled = b, true);
        Subs.CVar(_config, CCVars.AutoSavesDirectory, s => _directory = new ResPath(s), true);
    }

    private void OnIntervalChanged(int i)
    {
        _nextAutosave -= _interval;
        _interval = TimeSpan.FromMinutes(i);
        _nextAutosave += _interval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        if (!_firstMessageSent && _nextAutosave - _intervalMessage1 < _timing.RealTime)
        {
            _chat.DispatchServerAnnouncement(Loc.GetString("game-ticker-autosave-warning", ("minutes", _intervalMessage1.TotalMinutes)));
            _firstMessageSent = true;
        }

        if (!_secondMessageSent && _nextAutosave - _intervalMessage2 < _timing.RealTime)
        {
            _chat.DispatchServerAnnouncement(Loc.GetString("game-ticker-autosave-warning", ("minutes", _intervalMessage2.TotalMinutes)));
            _secondMessageSent = true;
        }

        if (_nextAutosave > _timing.RealTime)
            return;

        _nextAutosave = _timing.RealTime + _interval;

        Save();

        _firstMessageSent = false;
        _secondMessageSent = false;
    }

    [PublicAPI]
    public void Save()
    {
        var path = _directory / new ResPath($"{DateTime.Now:yyyy-M-dd_HH.mm.ss}-ROUND-{_ticker.RoundId}.{MapLoaderSystem.SaveExtension}");
        _mapLoader.TrySaveAllEntities(path);
    }

    [SubscribeLocalEvent]
    private void OnBeforeGameSave(BeforeSerializationEvent ev)
    {
        if (ev.Category != FileCategory.Save)
            return;

        _chat.DispatchServerAnnouncement(Loc.GetString("game-ticker-saving"));
    }

    [SubscribeLocalEvent]
    private void OnBeforeGameSave(AfterSerializationEvent ev)
    {
        if (ev.Category != FileCategory.Save)
            return;

        _chat.DispatchServerAnnouncement(Loc.GetString("game-ticker-saved"));
    }
}
