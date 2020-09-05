#nullable enable
using Content.Server.GameObjects.Components.Body;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System;
using System.Linq;

namespace Content.Server.Body
{
    class AddHandCommand : IClientCommand
    {
        public string Command => "addhand";
        public string Description => "Adds a hand to your entity.";
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

            if (!player.AttachedEntity.TryGetComponent(out BodyManagerComponent? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.SendText(player, text);
                return;
            }

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.TryIndex("bodyPart.LHand.BasicHuman", out BodyPartPrototype prototype);

            var part = new BodyPart(prototype);
            var slot = part.GetHashCode().ToString();

            body.Template.Slots.Add(slot, BodyPartType.Hand);
            body.TryAddPart(slot, part, true);
        }
    }

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

            if (!player.AttachedEntity.TryGetComponent(out BodyManagerComponent? body))
            {
                var random = IoCManager.Resolve<IRobustRandom>();
                var text = $"You have no body{(random.Prob(0.2f) ? " and you must scream." : ".")}";

                shell.SendText(player, text);
                return;
            }

            var hand = body.Parts.FirstOrDefault(x => x.Value.PartType == BodyPartType.Hand);
            if (hand.Value == null)
            {
                shell.SendText(player, "You have no hands.");
            }
            else
            {
                body.DisconnectBodyPart(hand.Value, true);
            }
        }
    }

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

            if (!player.AttachedEntity.TryGetComponent(out BodyManagerComponent? body))
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
                    part.DestroyMechanism(mechanism);
                    shell.SendText(player, $"Mechanism with name {mechanismName} has been destroyed.");
                    return;
                }
            }

            shell.SendText(player, $"No mechanism was found with name {mechanismName}.");
        }
    }

    class HurtCommand : IClientCommand
    {
        public string Command => "hurt";
        public string Description => "Ouch";
        public string Help => $"Usage: {Command} <type> <amount> (<entity uid/_>) (<ignoreResistance>)";

        private void SendDamageTypes(IConsoleShell shell, IPlayerSession? player)
        {
            var msg = "";
            foreach (var dClass in Enum.GetNames(typeof(DamageClass)))
            {
                msg += $"\n{dClass}";
                var types = Enum.Parse<DamageClass>(dClass).ToTypes();
                foreach (var dType in types)
                {
                    msg += $"\n - {dType}";
                }
            }
            shell.SendText(player, $"Damage Types:{msg}");
        }

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            // Check if we have enough for the dmg types to show
            if (args.Length > 0 && args[0] == "?")
            {
                SendDamageTypes(shell, player);
                return;
            }

            // Not enough args
            if (args.Length < 2)
            {
                shell.SendText(player, Help);
                return;
            }

            var ignoreResistance = false;
            var entityUid = player != null && player.AttachedEntityUid.HasValue ? player.AttachedEntityUid.Value : EntityUid.Invalid;
            if (!int.TryParse(args[1], out var amount) ||
                args.Length >= 3 && args[2] != "_" && !EntityUid.TryParse(args[2], out entityUid) || 
                args.Length >= 4 && !bool.TryParse(args[3], out ignoreResistance))
            {
                shell.SendText(player, Help);
                return;
            }

            if (entityUid == EntityUid.Invalid)
            {
                shell.SendText(player, "Not a valid entity.");
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetEntity(entityUid, out var ent))
            {
                shell.SendText(player, "Entity couldn't be found.");
                return;
            }

            if (!ent.TryGetComponent(out IDamageableComponent? damageable))
            {
                shell.SendText(player, "Entity can't be damaged.");
                return;
            }

            if (Enum.TryParse<DamageClass>(args[0], true, out var dmgClass))
            {
                if (!damageable.ChangeDamage(dmgClass, amount, ignoreResistance))
                    shell.SendText(player, "Something went wrong!");
                return;
            }
            // Fall back to DamageType
            else if (Enum.TryParse<DamageType>(args[0], true, out var dmgType))
            {
                if (!damageable.ChangeDamage(dmgType, amount, ignoreResistance))
                    shell.SendText(player, "Something went wrong!");
                return;
            }
            else
            {
                SendDamageTypes(shell, player);
            }
        }
    }
}
