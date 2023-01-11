using Content.Server.Administration;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Audio;
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

    private void PlayAdminGlobal(Filter playerFilter, string filename, AudioParams? audioParams = null, bool replay = true)
    {
        var msg = new AdminSoundEvent(filename, audioParams);
        RaiseNetworkEvent(msg, playerFilter, recordReplay: replay);
    }

    private Filter GetStationAndPvs(EntityUid source)
    {
        var stationFilter = _stationSystem.GetInOwningStation(source);
        stationFilter.AddPlayersByPvs(source, entityManager: EntityManager);
        return stationFilter;
    }

    public void PlayGlobalOnStation(EntityUid source, string filename, AudioParams? audioParams = null)
    {
        var msg = new GameGlobalSoundEvent(filename, audioParams);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    public void StopStationEventMusic(EntityUid source, StationEventMusicType type)
    {
        // TODO REPLAYS
        // these start & stop events are gonna be a PITA
        // theres probably some nice way of handling them. Maybe it just needs dedicated replay data (in which case these events should NOT get recorded).

        var msg = new StopStationEventMusic(type);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    public void DispatchStationEventMusic(EntityUid source, SoundSpecifier sound, StationEventMusicType type)
    {
        var audio = AudioParams.Default.WithVolume(-8);
        var soundFile = sound.GetSound();
        var msg = new StationEventMusicEvent(soundFile, type, audio);

        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    /// <summary>
    ///     Command that allows admins to play global sounds.
    /// </summary>
    [AdminCommand(AdminFlags.Fun)]
    public void PlayGlobalSoundCommand(IConsoleShell shell, string argStr, string[] args)
    {
        Filter filter;
        var audio = AudioParams.Default;

        bool replay = true;

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
                    // TODO REPLAYS uhhh.. what to do with this?
                    replay = false;

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

        audio = audio.AddVolume(-8);
        PlayAdminGlobal(filter, args[0], audio, replay);
    }
}
