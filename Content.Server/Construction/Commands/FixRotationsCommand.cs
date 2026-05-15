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
public sealed class FixRotationsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private static readonly ProtoId<TagPrototype> ForceFixRotationsTag = "ForceFixRotations";
    private static readonly ProtoId<TagPrototype> ForceNoFixRotationsTag = "ForceNoFixRotations";
    private static readonly ProtoId<TagPrototype> DiagonalTag = "Diagonal";

    // ReSharper disable once StringLiteralTypo
    public override string Command => "fixrotations";

    public override void Execute(IConsoleShell shell, string argsOther, string[] args)
    {
        var player = shell.Player;
        EntityUid? gridId;
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();

        switch (args.Length)
        {
            case 0:
                if (player?.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
                    return;
                }

                gridId = xformQuery.GetComponent(playerEntity).GridUid;
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var idNet) || !EntityManager.TryGetEntity(idNet, out var id))
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

        if (!EntityManager.TryGetComponent(gridId, out MapGridComponent? grid))
        {
            shell.WriteError(Loc.GetString("cmd-fixrotations-no-grid", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        if (!EntityManager.EntityExists(gridId))
        {
            shell.WriteError(Loc.GetString("cmd-fixrotations-grid-no-entity", ("gridId", (gridId?.ToString() ?? string.Empty))));
            return;
        }

        var changed = 0;

        var enumerator = xformQuery.GetComponent(gridId.Value).ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!EntityManager.EntityExists(child))
            {
                continue;
            }

            var valid = false;

            // Occluders should only count if the state of it right now is enabled.
            // This prevents issues with edge firelocks.
            if (EntityManager.TryGetComponent<OccluderComponent>(child, out var occluder))
            {
                valid |= occluder.Enabled;
            }
            // low walls & grilles
            valid |= EntityManager.HasComponent<SharedCanBuildWindowOnTopComponent>(child);
            // cables
            valid |= EntityManager.HasComponent<CableComponent>(child);
            // anything else that might need this forced
            valid |= _tagSystem.HasTag(child, ForceFixRotationsTag);
            // override
            valid &= !_tagSystem.HasTag(child, ForceNoFixRotationsTag);
            // remove diagonal entities as well
            valid &= !_tagSystem.HasTag(child, DiagonalTag);

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
