using System.Globalization;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Console;
using System.Linq;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition;

[AdminCommand(AdminFlags.Debug)]
public sealed class SetSatiation : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SatiationSystem _satiation = default!;

    public override string Command => "setsatiation";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (player.AttachedEntity is not { Valid: true } playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)
            ));
            return;
        }

        if (!EntityManager.TryGetComponent<SatiationComponent>(playerEntity, out var comp))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component",
                ("uid", playerEntity.Id),
                ("componentName", nameof(SatiationComponent))));
            return;
        }

        ProtoId<SatiationTypePrototype> type = args[0];
        if (comp.GetOrNull(type) is not { } satiation)
        {
            var typeName = _satiation.GetTypeOrNull(type)?.Name is { } locId ? Loc.GetString(locId) : $"\"{type}\"";
            shell.WriteLine(Loc.GetString(
                "shell-target-entity-does-not-have-message",
                ("missing", Loc.GetString("cmd-nutrition-setsatiation-need", ("satiation", typeName)))
            ));
            return;
        }

        if (!_proto.Resolve(satiation.Prototype, out var proto))
        {
            shell.WriteError(Loc.GetString(
                "cmd-nutrition-setsatiation-prototype-error",
                ("satiation-proto-id", satiation.Prototype)
            ));
            return;
        }

        if (float.TryParse(args[1], out var value))
        {
            _satiation.SetValue((playerEntity, comp), type, value);
            return;
        }

        if (!proto.Keys.TryGetValue(args[1], out var valueFromKey))
        {
            shell.WriteLine(Loc.GetString(
                "cmd-nutrition-setsatiation-no-matching-key-error",
                ("key", args[1]),
                ("satiation-proto-id", proto.ID)
            ));
            return;
        }

        _satiation.SetValue((playerEntity, comp), type, value: valueFromKey);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } playerEntity ||
            !EntityManager.TryGetComponent<SatiationComponent>(playerEntity, out var comp))
            return CompletionResult.Empty;

        var entity = new Entity<SatiationComponent>(playerEntity, comp);

        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(
                    entity.Comp.Satiations.Keys.Select(it => it.Id),
                    nameof(SatiationTypePrototype)
                );
            case 2:
                if (_satiation.GetTypeOrNull(args[0]) is not { } satiationProto ||
                    _satiation.GetMaximumValue(entity, satiationProto) is not { } maxValue)
                    return CompletionResult.Empty;

                var keys = _satiation.GetKeysForType(entity, satiationProto)
                    .Select(key =>
                        new CompletionOption(key, Loc.GetString("cmd-nutrition-setsatiation-hint-key")));

                return CompletionResult.FromHintOptions(
                    keys.Concat(
                        [
                            new CompletionOption(maxValue.ToString(CultureInfo.InvariantCulture),
                                Loc.GetString("cmd-nutrition-setsatiation-hint-max-value"))
                        ]
                    ),
                    hint: null
                );
            default:
                return CompletionResult.Empty;
        }
    }
}
