using Content.Client.Markers;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[UsedImplicitly]
internal sealed class MappingClientSideSetupCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    // ReSharper disable once StringLiteralTypo
    public string Command => "mappingclientsidesetup";
    public string Description => Loc.GetString("mapping-client-side-setup-command-description");
    public string Help => Loc.GetString("mapping-client-side-setup-command-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_lightManager.LockConsoleAccess)
        {
            _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible = true;
            _lightManager.Enabled = false;
            // ReSharper disable once StringLiteralTypo
            shell.ExecuteCommand("showsubfloorforever");
            // ReSharper disable once StringLiteralTypo
            shell.ExecuteCommand("loadmapacts");
        }
    }
}

