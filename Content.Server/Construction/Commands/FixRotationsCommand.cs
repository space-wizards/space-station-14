using Content.Server.Administration;
using Content.Server.Power.Components;
using Content.Shared.Administration;
using Content.Shared.Construction;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class FixRotationsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private static readonly ProtoId<TagPrototype> ForceFixRotationsTag = "ForceFixRotations";
    private static readonly ProtoId<TagPrototype> ForceNoFixRotationsTag = "ForceNoFixRotations";
    private static readonly ProtoId<TagPrototype> DiagonalTag = "Diagonal";

    // ReSharper disable once StringLiteralTypo
    public string Command => "fixrotations";
    public string Description => Loc.GetString("cmd-fixrotations-desc");
    public string Help => Loc.GetString("cmd-fixrotations-help");

    public void Execute(IConsoleShell shell, string argsOther, string[] args)
    {
        var player = shell.Player;
        EntityUid? gridId;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        switch (args.Length)
        {
            case 0:
                if (player?.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError(Loc.GetString("cmd-fixrotations-only-player"));
                    return;
                }

                gridId = xformQuery.GetComponent(playerEntity).GridUid;
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var idNet) || !_entManager.TryGetEntity(idNet, out var id))
                {
                    shell.WriteError(Loc.GetString("cmd-fixrotations-invalid-entity", ("entity", args[0])));
                    return;
                }

                gridId = id;
                break;
            default:
                shell.WriteLine(Help);
                return;
        }

        if (!_entManager.TryGetComponent(gridId, out MapGridComponent? grid))
        {
            shell.WriteError(Loc.GetString("cmd-fixrotations-no-grid", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        if (!_entManager.EntityExists(gridId))
        {
            shell.WriteError(Loc.GetString("cmd-fixrotations-grid-no-entity", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        var changed = 0;
        var tagSystem = _entManager.EntitySysManager.GetEntitySystem<TagSystem>();


        var enumerator = xformQuery.GetComponent(gridId.Value).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!_entManager.EntityExists(child))
            {
                continue;
            }

            var valid = false;

            // Occluders should only count if the state of it right now is enabled.
            // This prevents issues with edge firelocks.
            if (_entManager.TryGetComponent<OccluderComponent>(child, out var occluder))
            {
                valid |= occluder.Enabled;
            }
            // low walls & grilles
            valid |= _entManager.HasComponent<SharedCanBuildWindowOnTopComponent>(child);
            // cables
            valid |= _entManager.HasComponent<CableComponent>(child);
            // anything else that might need this forced
            valid |= tagSystem.HasTag(child, ForceFixRotationsTag);
            // override
            valid &= !tagSystem.HasTag(child, ForceNoFixRotationsTag);
            // remove diagonal entities as well
            valid &= !tagSystem.HasTag(child, DiagonalTag);

            if (!valid)
                continue;

            var childXform = xformQuery.GetComponent(child);

            if (childXform.LocalRotation != Angle.Zero)
            {
                childXform.LocalRotation = Angle.Zero;
                changed++;
            }
        }

        shell.WriteLine(Loc.GetString("cmd-fixrotations-changed", ("changed", changed)));
    }
}
