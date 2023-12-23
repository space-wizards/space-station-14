using Content.Client.Markers;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Commands;

/// <summary>
/// Sent by mapping command to client.
/// This is because the debug commands for some of these options are on toggles.
/// </summary>
[UsedImplicitly]
internal sealed class MappingClientSideSetupCommand : IConsoleCommand
{
    // ReSharper disable once StringLiteralTypo
    public string Command => "mappingclientsidesetup";
    public string Description => Loc.GetString("mapping-client-side-setup-command-description");
    public string Help => Loc.GetString("mapping-client-side-setup-command-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var mgr = IoCManager.Resolve<ILightManager>();
        if (!mgr.LockConsoleAccess)
        {
            EntitySystem.Get<MarkerSystem>().MarkersVisible = true;
            mgr.Enabled = false;
            shell.ExecuteCommand("showsubfloorforever");
            shell.ExecuteCommand("loadmapacts");
        }
    }
}

