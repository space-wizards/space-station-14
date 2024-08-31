using Content.Client.Actions;
using Content.Client.Mapping;
using Content.Client.Markers;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[UsedImplicitly]
internal sealed class MappingClientSideSetupCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    public override string Command => "mappingclientsidesetup";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_lightManager.LockConsoleAccess)
        {
            _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible = true;
            _lightManager.Enabled = false;
            shell.ExecuteCommand("showsubfloorforever");
            _entitySystemManager.GetEntitySystem<ActionsSystem>().LoadActionAssignments("/mapping_actions.yml", false);
        }
    }
}

