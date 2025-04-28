using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;

namespace Content.Client.Access.Commands;

public sealed class ShowAccessReadersCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IResourceCache _cahce = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override string Command => "showaccessreaders";

    public override string Description => "Toggles showing access reader permissions on the map";
    public override string Help => """
        Overlay Info:
        -Disabled | The access reader is disabled
        +Unrestricted | The access reader has no restrictions
        +Set [Index]: [Tag Name]| A tag in an access set (accessor needs all tags in the set to be allowed by the set)
        +Key [StationUid]: [StationRecordKeyId] | A StationRecordKey that is allowed
        -Tag [Tag Name] | A tag that is not allowed (takes priority over other allows)
        """;
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlay.RemoveOverlay<AccessOverlay>())
        {
            shell.WriteLine(Loc.GetString($"cmd-show-access-readers-disabled"));
            return;
        }

        _overlay.AddOverlay(new AccessOverlay(_ent, _cahce, _xform));
        shell.WriteLine(Loc.GetString($"cmd-show-access-readers-enabled"));

    }
}
