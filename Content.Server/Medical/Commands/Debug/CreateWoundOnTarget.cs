using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Commands.Debug;

[AdminCommand(AdminFlags.Debug)]
public sealed class CreateWoundOnTarget : LocalizedCommands
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override string Command { get; } = "CreateWoundOnTarget";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteError("Incorrect arguments");
            return;
        }

        if (!int.TryParse(args[0], out var rawId))
        {
            shell.WriteError("Target entityId is not an number");
            return;
        }

        var target = _entityManager.GetEntity(new NetEntity(rawId));
        if (!target.IsValid())
        {
            shell.WriteError("Target EntityId is invalid");
            return;
        }

        if (!_prototypeManager.HasIndex(args[1]))
        {
            shell.WriteError("Wound prototypeId is invalid");
            return;
        }

        CreateWound(target, args[1], null ,shell);
    }
    private void CreateWound(EntityUid target, string protoId, EntityUid? woundableEnt, IConsoleShell shell)
    {
        if (!_entityManager.TrySystem(out SharedBodySystem? bodySystem) ||
            !_entityManager.TrySystem(out WoundSystem? woundSystem) ||
            !_entityManager.TryGetComponent(target, out BodyComponent? body))
            return;
        shell.WriteLine($"Creating wound on Entity:{target}");
        if (woundableEnt == null)
        {
            shell.WriteLine($"Target woundable not specified using RootPart!");
            if (body.RootContainer.ContainedEntity == null)
            {
                shell.WriteLine($"Target does not have a rootPart!");
                return;
            }
            if (!_entityManager.TryGetComponent<WoundableComponent>(body.RootContainer.ContainedEntity,
                    out var woundable))
            {
                shell.WriteLine($"RootPart Entity:{body.RootContainer.ContainedEntity} is not Woundable. Failed to create Wound!");
                return;
            }
            woundableEnt = body.RootContainer.ContainedEntity;
            if (!woundSystem.CreateWoundOnWoundable(
                    new(woundableEnt.Value, woundable), protoId))
            {
                shell.WriteLine($"Failed to create Wound!");
                return;
            }
        }
        else
        {
            if (!_entityManager.TryGetComponent<WoundableComponent>(woundableEnt.Value, out var woundable2))
            {
                shell.WriteLine($"Target Entity:{body.RootContainer.ContainedEntity} is not Woundable. Failed to create Wound!");
                return;
            }

            if (!woundSystem.CreateWoundOnWoundable(
                    new(woundableEnt.Value, woundable2), protoId))
            {
                shell.WriteLine($"Failed to create Wound!");
                return;
            }
        }
        shell.WriteLine($"{protoId} Wound created on Entity{woundableEnt}!");
    }
}

