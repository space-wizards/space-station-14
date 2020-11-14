#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Body
{
    [AdminCommand(AdminFlags.Fun)]
    class AddHandCommand : IClientCommand
    {
        public const string DefaultHandPrototype = "LeftHandHuman";

        public string Command => "addhand";
        public string Description => "Adds a hand to your entity.";
        public string Help => $"Usage: {Command} <entityUid> <handPrototypeId> / {Command} <entityUid> / {Command} <handPrototypeId> / {Command}";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length > 1)
            {
                shell.SendText(player, Help);
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            IEntity entity;
            IEntity hand;

            switch (args.Length)
            {
                case 0:
                {
                    if (player == null)
                    {
                        shell.SendText(player, "Only a player can run this command without arguments.");
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.SendText(player, "You don't have an entity to add a hand to.");
                        return;
                    }

                    entity = player.AttachedEntity;
                    hand = entityManager.SpawnEntity(DefaultHandPrototype, entity.Transform.Coordinates);
                    break;
                }
                case 1:
                {
                    if (EntityUid.TryParse(args[0], out var uid))
                    {
                        if (!entityManager.TryGetEntity(uid, out var parsedEntity))
                        {
                            shell.SendText(player, $"No entity found with uid {uid}");
                            return;
                        }

                        entity = parsedEntity;
                        hand = entityManager.SpawnEntity(DefaultHandPrototype, entity.Transform.Coordinates);
                    }
                    else
                    {
                        if (player == null)
                        {
                            shell.SendText(player,
                                "You must specify an entity to add a hand to when using this command from the server terminal.");
                            return;
                        }

                        if (player.AttachedEntity == null)
                        {
                            shell.SendText(player, "You don't have an entity to add a hand to.");
                            return;
                        }

                        entity = player.AttachedEntity;
                        hand = entityManager.SpawnEntity(args[0], entity.Transform.Coordinates);
                    }

                    break;
                }
                case 2:
                {
                    if (!EntityUid.TryParse(args[0], out var uid))
                    {
                        shell.SendText(player, $"{args[0]} is not a valid entity uid.");
                        return;
                    }

                    if (!entityManager.TryGetEntity(uid, out var parsedEntity))
                    {
                        shell.SendText(player, $"No entity exists with uid {uid}.");
                        return;
                    }

                    entity = parsedEntity;

                    if (!prototypeManager.HasIndex<EntityPrototype>(args[1]))
                    {
                        shell.SendText(player, $"No hand entity exists with id {args[1]}.");
                        return;
                    }

                    hand = entityManager.SpawnEntity(args[1], entity.Transform.Coordinates);

                    break;
                }
                default:
                {
                    shell.SendText(player, Help);
                    return;
                }
            }

            if (!entity.TryGetComponent(out IBody? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.SendText(player, text);
                return;
            }

            if (!hand.TryGetComponent(out IBodyPart? part))
            {
                shell.SendText(player, $"Hand entity {hand} does not have a {nameof(IBodyPart)} component.");
                return;
            }

            var slot = part.GetHashCode().ToString();
            var response = body.TryAddPart(slot, part, true)
                ? $"Added hand to entity {entity.Name}"
                : $"Error occurred trying to add a hand to entity {entity.Name}";

            shell.SendText(player, response);
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    class RemoveHandCommand : IClientCommand
    {
        public string Command => "removehand";
        public string Description => "Removes a hand from your entity.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "Only a player can run this command.");
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.SendText(player, "You have no entity.");
                return;
            }

            if (!player.AttachedEntity.TryGetComponent(out IBody? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.SendText(player, text);
                return;
            }

            var hand = body.Parts.FirstOrDefault(x => x.Value.PartType == BodyPartType.Hand);
            if (hand.Value.Equals(default))
            {
                shell.SendText(player, "You have no hands.");
            }
            else
            {
                body.RemovePart(hand.Value);
            }
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    class DestroyMechanismCommand : IClientCommand
    {
        public string Command => "destroymechanism";
        public string Description => "Destroys a mechanism from your entity";
        public string Help => $"Usage: {Command} <mechanism>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "Only a player can run this command.");
                return;
            }

            if (args.Length == 0)
            {
                shell.SendText(player, Help);
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.SendText(player, "You have no entity.");
                return;
            }

            if (!player.AttachedEntity.TryGetComponent(out IBody? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.SendText(player, text);
                return;
            }

            var mechanismName = string.Join(" ", args).ToLowerInvariant();

            foreach (var part in body.Parts.Values)
            foreach (var mechanism in part.Mechanisms)
            {
                if (mechanism.Name.ToLowerInvariant() == mechanismName)
                {
                    part.DeleteMechanism(mechanism);
                    shell.SendText(player, $"Mechanism with name {mechanismName} has been destroyed.");
                    return;
                }
            }

            shell.SendText(player, $"No mechanism was found with name {mechanismName}.");
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    class HurtCommand : IClientCommand
    {
        public string Command => "hurt";
        public string Description => "Ouch";
        public string Help => $"Usage: {Command} <type/?> <amount> (<entity uid/_>) (<ignoreResistances>)";

        private string DamageTypes()
        {
            var msg = "";
            foreach (var dClass in Enum.GetNames(typeof(DamageClass)))
            {
                msg += $"\n{dClass}";
                var types = Enum.Parse<DamageClass>(dClass).ToTypes();

                if (types.Count > 0)
                {
                    msg += ": ";
                }

                msg += string.Join('|', types);
            }

            return $"Damage Types:{msg}";
        }

        private delegate void Damage(IDamageableComponent damageable, bool ignoreResistances);

        private bool TryParseEntity(IConsoleShell shell, IPlayerSession? player, string arg,
            [NotNullWhen(true)] out IEntity? entity)
        {
            entity = null;

            if (arg == "_")
            {
                var playerEntity = player?.AttachedEntity;

                if (playerEntity == null)
                {
                    shell.SendText(player, $"You must have a player entity to use this command without specifying an entity.\n{Help}");
                    return false;
                }

                entity = playerEntity;
                return true;
            }

            if (!EntityUid.TryParse(arg, out var entityUid))
            {
                shell.SendText(player, $"{arg} is not a valid entity uid.\n{Help}");

                return false;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(entityUid, out var parsedEntity))
            {
                shell.SendText(player, $"No entity found with uid {entityUid}");

                return false;
            }

            entity = parsedEntity;
            return true;
        }

        private bool TryParseDamageArgs(
            IConsoleShell shell,
            IPlayerSession? player,
            string[] args,
            [NotNullWhen(true)] out Damage? func)
        {
            if (!int.TryParse(args[1], out var amount))
            {
                shell.SendText(player, $"{args[1]} is not a valid damage integer.");

                func = null;
                return false;
            }

            if (Enum.TryParse<DamageClass>(args[0], true, out var damageClass))
            {
                func = (damageable, ignoreResistances) =>
                {
                    if (!damageable.DamageClasses.ContainsKey(damageClass))
                    {
                        shell.SendText(player, $"Entity {damageable.Owner.Name} with id {damageable.Owner.Uid} can not be damaged with damage class {damageClass}");

                        return;
                    }

                    if (!damageable.ChangeDamage(damageClass, amount, ignoreResistances))
                    {
                        shell.SendText(player, $"Entity {damageable.Owner.Name} with id {damageable.Owner.Uid} received no damage.");

                        return;
                    }

                    var response =
                        $"Damaged entity {damageable.Owner.Name} with id {damageable.Owner.Uid} for {amount} {damageClass} damage{(ignoreResistances ? ", ignoring resistances." : ".")}";

                    shell.SendText(player, response);
                };

                return true;
            }
            // Fall back to DamageType
            else if (Enum.TryParse<DamageType>(args[0], true, out var damageType))
            {
                func = (damageable, ignoreResistances) =>
                {
                    if (!damageable.DamageTypes.ContainsKey(damageType))
                    {
                        shell.SendText(player, $"Entity {damageable.Owner.Name} with id {damageable.Owner.Uid} can not be damaged with damage class {damageType}");

                        return;
                    }

                    if (!damageable.ChangeDamage(damageType, amount, ignoreResistances))
                    {
                        shell.SendText(player, $"Entity {damageable.Owner.Name} with id {damageable.Owner.Uid} received no damage.");

                        return;
                    }

                    var response =
                        $"Damaged entity {damageable.Owner.Name} with id {damageable.Owner.Uid} for {amount} {damageType} damage{(ignoreResistances ? ", ignoring resistances." : ".")}";

                    shell.SendText(player, response);
                };

                return true;
            }
            else
            {
                shell.SendText(player, $"{args[0]} is not a valid damage class or type.");

                var types = DamageTypes();
                shell.SendText(player, types);

                func = null;
                return false;
            }
        }

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            bool ignoreResistances;
            IEntity entity;
            Damage? damageFunc;

            switch (args.Length)
            {
                // Check if we have enough for the dmg types to show
                case var n when n > 0 && (args[0] == "?" || args[0] == "¿"):
                    var types = DamageTypes();

                    if (args[0] == "¿")
                    {
                        types = types.Replace("e", "é");
                    }

                    shell.SendText(player, types);

                    return;
                // Not enough args
                case var n when n < 2:
                    shell.SendText(player, $"Invalid number of arguments.\n{Help}");
                    return;
                case var n when n >= 2 && n <= 4:
                    if (!TryParseDamageArgs(shell, player, args, out damageFunc))
                    {
                        return;
                    }

                    var entityUid = n == 2 ? "_" : args[2];

                    if (!TryParseEntity(shell, player, entityUid, out var parsedEntity))
                    {
                        return;
                    }

                    entity = parsedEntity;

                    if (n == 4)
                    {
                        if (!bool.TryParse(args[3], out ignoreResistances))
                        {
                            shell.SendText(player, $"{args[3]} is not a valid boolean value for ignoreResistances.\n{Help}");
                            return;
                        }
                    }
                    else
                    {
                        ignoreResistances = false;
                    }

                    break;
                default:
                    shell.SendText(player, $"Invalid amount of arguments.\n{Help}");
                    return;
            }

            if (!entity.TryGetComponent(out IDamageableComponent? damageable))
            {
                shell.SendText(player, $"Entity {entity.Name} with id {entity.Uid} does not have a {nameof(IDamageableComponent)}.");
                return;
            }

            damageFunc(damageable, ignoreResistances);
        }
    }
}
