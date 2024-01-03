using Content.Client.Mapping;
using Content.Client.Markers;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
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
            IoCManager.Resolve<IStateManager>().RequestStateChange<MappingState>();
        }
    }
}

