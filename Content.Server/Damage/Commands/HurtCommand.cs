using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    sealed class DamageCommand : IConsoleCommand
    {
        public string Command => "damage";
        public string Description => Loc.GetString("damage-command-description");
        public string Help => Loc.GetString("damage-command-help", ("command", Command));

        private readonly IPrototypeManager _prototypeManager = default!;
        public DamageCommand() {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var types = _prototypeManager.EnumeratePrototypes<DamageTypePrototype>()
                    .Select(p => new CompletionOption(p.ID));

                var groups = _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>()
                    .Select(p => new CompletionOption(p.ID));

                return CompletionResult.FromHintOptions(types.Concat(groups).OrderBy(p => p.Value),
                    Loc.GetString("damage-command-arg-type"));
            }

            if (args.Length == 2)
            {
                return CompletionResult.FromHint(Loc.GetString("damage-command-arg-quantity"));
            }

            if (args.Length == 3)
            {
                // if type.Name is good enough for cvars, <bool> doesn't need localizing.
                return CompletionResult.FromHint("<bool>");
            }

            if (args.Length == 4)
            {
                return CompletionResult.FromHint(Loc.GetString("damage-command-arg-target"));
            }

            return CompletionResult.Empty;
        }

        private delegate void Damage(EntityUid entity, bool ignoreResistances);

        private bool TryParseDamageArgs(
            IConsoleShell shell,
            EntityUid target,
            string[] args,
            [NotNullWhen(true)] out Damage? func)
        {
            if (!float.TryParse(args[1], out var amount))
            {
                shell.WriteLine(Loc.GetString("damage-command-error-quantity", ("arg", args[1])));
                func = null;
                return false;
            }

            if (_prototypeManager.TryIndex<DamageGroupPrototype>(args[0], out var damageGroup))
            {
                func = (entity, ignoreResistances) =>
                {
                    var damage = new DamageSpecifier(damageGroup, amount);
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(entity, damage, ignoreResistances);
                };

                return true;
            }
            // Fall back to DamageType
            else if (_prototypeManager.TryIndex<DamageTypePrototype>(args[0], out var damageType))
            {
                func = (entity, ignoreResistances) =>
                {
                    var damage = new DamageSpecifier(damageType, amount);
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(entity, damage, ignoreResistances);
                };
                return true;

            }
            else
            {
                shell.WriteLine(Loc.GetString("damage-command-error-type", ("arg", args[0])));
                func = null;
                return false;
            }
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2 || args.Length > 4)
            {
                shell.WriteLine(Loc.GetString("damage-command-error-args"));
                return;
            }

            EntityUid target;
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (args.Length == 4)
            {
                if (!EntityUid.TryParse(args[3], out target) || !entMan.EntityExists(target))
                {
                    shell.WriteLine(Loc.GetString("damage-command-error-euid", ("arg", args[3])));
                    return;
                }
            }
            else if (shell.Player?.AttachedEntity is { Valid: true } playerEntity)
            {
                target = playerEntity;
            }
            else
            {
                shell.WriteLine(Loc.GetString("damage-command-error-player"));
                return;
            }

            if (!TryParseDamageArgs(shell, target, args, out var damageFunc))
                return;

            bool ignoreResistances;
            if (args.Length == 3)
            {
                if (!bool.TryParse(args[2], out ignoreResistances))
                {
                    shell.WriteLine(Loc.GetString("damage-command-error-bool", ("arg", args[2])));
                    return;
                }
            }
            else
            {
                ignoreResistances = false;
            }

            damageFunc(target, ignoreResistances);
        }
    }
}
