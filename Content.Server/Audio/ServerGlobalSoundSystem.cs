using Content.Server.Administration;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Audio;
using Content.Shared.Sound;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Audio;

public sealed class ServerGlobalSoundSystem : SharedGlobalSoundSystem
{
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        _conHost.RegisterCommand("playglobalsound", Loc.GetString("play-global-sound-command-description"), Loc.GetString("play-global-sound-command-help"), PlayGlobalSoundCommand);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _conHost.UnregisterCommand("playglobalsound");
    }

    private void PlayAdminGlobal(Filter playerFilter, string filename, AudioParams? audioParams = null)
    {
        var msg = new AdminSoundEvent(filename, audioParams);
        RaiseNetworkEvent(msg, playerFilter);
    }

    public void PlayGlobalOnStation(EntityUid source, string filename, AudioParams? audioParams = null)
    {
        var msg = new GameGlobalSoundEvent(filename, audioParams);
        foreach(var filter in GetStationFilters(source))
        {
            RaiseNetworkEvent(msg, filter);
        }
    }

    private void PlayStationEventMusic(Filter playerFilter, string filename, StationEventMusicType type, AudioParams? audioParams = null)
    {
        var msg = new StationEventMusicEvent(filename, type, audioParams);
        RaiseNetworkEvent(msg, playerFilter);
    }

    public void StopStationEventMusic(EntityUid source, StationEventMusicType type)
    {
        var msg = new StopStationEventMusic(type);
        foreach (var filter in GetStationFilters(source))
        {
            RaiseNetworkEvent(msg, filter);
        }
    }

    public void DispatchStationEventMusic(EntityUid source, SoundSpecifier sound, StationEventMusicType type)
    {
        var audio = AudioParams.Default.WithVolume(-8);
        var soundFile = sound.GetSound();

        foreach (var filter in GetStationFilters(source))
        {
            PlayStationEventMusic(filter, soundFile, type, audio);
        }
    }

    private List<Filter> GetStationFilters(EntityUid source)
    {
        var station = _stationSystem.GetOwningStation(source);
        if (station != null)
        {
            if(!EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp)) return new List<Filter>(1) {  Filter.Pvs(source) };
            var filters = new List<Filter>(stationDataComp.Grids.Count);
            foreach (var gridEnt in stationDataComp.Grids)
            {
                filters.Add(Filter.BroadcastGrid(gridEnt));
            }
        }
        return new List<Filter>(1) {  Filter.Pvs(source) };
    }

    /// <summary>
    ///     Command that allows admins to play global sounds.
    /// </summary>
    [AdminCommand(AdminFlags.Fun)]
    public void PlayGlobalSoundCommand(IConsoleShell shell, string argStr, string[] args)
    {
        Filter filter;
        var audio = AudioParams.Default.WithVolume(-8);

        switch (args.Length)
        {
            // No arguments, show command help.
            case 0:
                shell.WriteLine(Loc.GetString("play-global-sound-command-help"));
                return;

            // No users, play sound for everyone.
            case 1:
                // Filter.Broadcast does resolves IPlayerManager, so use this instead.
                filter = Filter.Empty().AddAllPlayers(_playerManager);
                break;

            // One or more users specified.
            default:
                var volumeOffset = 0;

                // Try to specify a new volume to play it at.
                if (int.TryParse(args[1], out var volume))
                {
                    audio = audio.WithVolume(volume);
                    volumeOffset = 1;
                }
                else
                {
                    shell.WriteError(Loc.GetString("play-global-sound-command-volume-parse", ("volume", args[1])));
                    return;
                }

                // No users specified so play for them all.
                if (args.Length == 2)
                {
                    filter = Filter.Empty().AddAllPlayers(_playerManager);
                }
                else
                {
                    filter = Filter.Empty();

                    // Skip the first argument, which is the sound path.
                    for (var i = 1 + volumeOffset; i < args.Length; i++)
                    {
                        var username = args[i];

                        if (!_playerManager.TryGetSessionByUsername(username, out var session))
                        {
                            shell.WriteError(Loc.GetString("play-global-sound-command-player-not-found", ("username", username)));
                            continue;
                        }

                        filter.AddPlayer(session);
                    }
                }

                break;
        }

        PlayAdminGlobal(filter, args[0], audio);
    }
}
