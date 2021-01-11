#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Damage
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
}
