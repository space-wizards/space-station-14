#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Damage
{
    public abstract class DamageFlagCommand : IClientCommand
    {
        public abstract string Command { get; }
        public abstract string Description { get; }
        public abstract string Help { get; }

        public abstract void Execute(IConsoleShell shell, IPlayerSession? player, string[] args);

        public bool TryGetEntity(
            IConsoleShell shell,
            IPlayerSession? player,
            string[] args,
            bool adding,
            [NotNullWhen(true)] out IEntity? entity,
            out DamageFlag flag,
            [NotNullWhen(true)] out IDamageableComponent? damageable)
        {
            entity = null;
            flag = DamageFlag.None;
            damageable = null;

            IEntity? parsedEntity;
            DamageFlag parsedFlag;
            IDamageableComponent? parsedDamageable;

            switch (args.Length)
            {
                case 1:
                {
                    if (player == null)
                    {
                        shell.SendText(player, "An entity needs to be specified when the command isn't used by a player.");
                        return false;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.SendText(player, "An entity needs to be specified when you aren't attached to an entity.");
                        return false;
                    }

                    if (!Enum.TryParse(args[0], true, out parsedFlag))
                    {
                        shell.SendText(player, $"{args[0]} is not a valid damage flag.");
                        return false;
                    }

                    parsedEntity = player.AttachedEntity;
                    flag = parsedFlag;
                    break;
                }
                case 2:
                {
                    if (!EntityUid.TryParse(args[0], out var id))
                    {
                        shell.SendText(player, $"{args[0]} isn't a valid entity id.");
                        return false;
                    }

                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    if (!entityManager.TryGetEntity(id, out parsedEntity))
                    {
                        shell.SendText(player, $"No entity found with id {id}.");
                        return false;
                    }

                    if (!Enum.TryParse(args[1], true, out parsedFlag))
                    {
                        shell.SendText(player, $"{args[1]} is not a valid damage flag.");
                        return false;
                    }

                    break;
                }
                default:
                    shell.SendText(player, Help);
                    return false;
            }

            if (!parsedEntity.TryGetComponent(out parsedDamageable))
            {
                shell.SendText(player, $"Entity {parsedEntity.Name} doesn't have a {nameof(IDamageableComponent)}");
                return false;
            }

            if (parsedDamageable.HasFlag(parsedFlag) && adding)
            {
                shell.SendText(player, $"Entity {parsedEntity.Name} already has damage flag {parsedFlag}.");
                return false;
            }
            else if (!parsedDamageable.HasFlag(parsedFlag) && !adding)
            {
                shell.SendText(player, $"Entity {parsedEntity.Name} doesn't have damage flag {parsedFlag}.");
                return false;
            }

            entity = parsedEntity;
            flag = parsedFlag;
            damageable = parsedDamageable;

            return true;
        }
    }

    public class AddDamageFlagCommand : DamageFlagCommand
    {
        public override string Command => "adddamageflag";
        public override string Description => "Adds a damage flag to your entity or another.";
        public override string Help => $"Usage: {Command} <flag> / {Command} <entityUid> <flag>";

        public override void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (!TryGetEntity(shell, player, args, true, out var entity, out var flag, out var damageable))
            {
                return;
            }

            damageable.AddFlag(flag);
            shell.SendText(player, $"Added damage flag {flag} to entity {entity.Name}");
        }
    }

    public class RemoveDamageFlagCommand : DamageFlagCommand
    {
        public override string Command => "removedamageflag";
        public override string Description => "Removes a damage flag from your entity or another.";
        public override string Help => $"Usage: {Command} <flag> / {Command} <entityUid> <flag>";

        public override void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (!TryGetEntity(shell, player, args, false, out var entity, out var flag, out var damageable))
            {
                return;
            }

            damageable.RemoveFlag(flag);
            shell.SendText(player, $"Removed damage flag {flag} from entity {entity.Name}");
        }
    }

    public class GodModeCommand : IClientCommand
    {
        public string Command => "godmode";
        public string Description => "Makes your entity or another invulnerable to almost anything. May have irreversible changes.";
        public string Help => $"Usage: {Command} / {Command} <entityUid>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            IEntity entity;

            switch (args.Length)
            {
                case 0:
                    if (player == null)
                    {
                        shell.SendText(player, "An entity needs to be specified when the command isn't used by a player.");
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.SendText(player, "An entity needs to be specified when you aren't attached to an entity.");
                        return;
                    }

                    entity = player.AttachedEntity;
                    break;
                case 1:
                    if (!EntityUid.TryParse(args[0], out var id))
                    {
                        shell.SendText(player, $"{args[0]} isn't a valid entity id.");
                        return;
                    }

                    var entityManager = IoCManager.Resolve<IEntityManager>();
                    if (!entityManager.TryGetEntity(id, out var parsedEntity))
                    {
                        shell.SendText(player, $"No entity found with id {id}.");
                        return;
                    }

                    entity = parsedEntity;
                    break;
                default:
                    shell.SendText(player, Help);
                    return;
            }

            if (entity.HasComponent<MovedByPressureComponent>())
            {
                entity.RemoveComponent<MovedByPressureComponent>();
            }

            if (entity.TryGetComponent(out IDamageableComponent? damageable))
            {
                damageable.AddFlag(DamageFlag.Invulnerable);
            }

            shell.SendText(player, $"Enabled godmode for entity {entity.Name}");
        }
    }
}
