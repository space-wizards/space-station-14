using JetBrains.Annotations;
using System;
using Content.Client.Markers;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

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
    public string Description => "Sets up the lighting control and such settings client-side. Sent by 'mapping' to client.";
    public string Help => "";

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

