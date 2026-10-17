using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Audio;

/// <inheritdoc/>
public sealed partial class ServerGlobalSoundSystem : GlobalSoundSystem
{
    [Dependency] private IConsoleHost _conHost = default!;

    /// <inheritdoc/>
    public override void Shutdown()
    {
        base.Shutdown();
        _conHost.UnregisterCommand("playglobalsound");
    }

    /// <summary>
    /// Plays an admin sound for the selected players and optionally records it for replay.
    /// </summary>
    /// <param name="playerFilter">Players who should receive the sound.</param>
    /// <param name="specifier">Resolved sound to play.</param>
    /// <param name="audioParams">Optional parameters controlling playback.</param>
    /// <param name="replay">Whether the network event should be recorded for replay.</param>
    [PublicAPI]
    public void PlayAdminGlobal(Filter playerFilter, ResolvedSoundSpecifier specifier, AudioParams? audioParams = null, bool replay = true)
    {
        var msg = new AdminSoundEvent(specifier, audioParams);
        RaiseNetworkEvent(msg, playerFilter, recordReplay: replay);
    }
}
