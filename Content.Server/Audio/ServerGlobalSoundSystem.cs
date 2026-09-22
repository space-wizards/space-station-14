using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Audio;

public sealed partial class ServerGlobalSoundSystem : GlobalSoundSystem
{
    [Dependency] private IConsoleHost _conHost = default!;

    public override void Shutdown()
    {
        base.Shutdown();
        _conHost.UnregisterCommand("playglobalsound");
    }

    public void PlayAdminGlobal(Filter playerFilter, ResolvedSoundSpecifier specifier, AudioParams? audioParams = null, bool replay = true)
    {
        var msg = new AdminSoundEvent(specifier, audioParams);
        RaiseNetworkEvent(msg, playerFilter, recordReplay: replay);
    }
}
