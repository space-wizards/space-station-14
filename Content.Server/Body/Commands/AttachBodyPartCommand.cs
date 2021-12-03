using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class AttachBodyPartCommand : IConsoleCommand
    {
        public string Command => "attachbodypart";
        public string Description => "Attaches a body part to you or someone else.";
        public string Help => $"{Command} <partEntityUid> / {Command} <entityUid> <partEntityUid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var entityManager = IoCManager.Resolve<IEntityManager>();

            IEntity entity;
            EntityUid partUid;

            switch (args.Length)
            {
                case 1:
                    if (player == null)
                    {
                        shell.WriteLine($"You need to specify an entity to attach the part to if you aren't a player.\n{Help}");
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.WriteLine($"You need to specify an entity to attach the part to if you aren't attached to an entity.\n{Help}");
                        return;
                    }

                    if (!EntityUid.TryParse(args[0], out partUid))
                    {
                        shell.WriteLine($"{args[0]} is not a valid entity uid.");
                        return;
                    }

                    entity = player.AttachedEntity;

                    break;
                case 2:
                    if (!EntityUid.TryParse(args[0], out var entityUid))
                    {
                        shell.WriteLine($"{args[0]} is not a valid entity uid.");
                        return;
                    }

                    if (!EntityUid.TryParse(args[1], out partUid))
                    {
                        shell.WriteLine($"{args[1]} is not a valid entity uid.");
                        return;
                    }

                    if (!entityManager.TryGetEntity(entityUid, out var tempEntity))
                    {
                        shell.WriteLine($"{entityUid} is not a valid entity.");
                        return;
                    }

                    entity = tempEntity;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SharedBodyComponent? body))
            {
                shell.WriteLine($"Entity {IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityName} with uid {entity} does not have a {nameof(SharedBodyComponent)} component.");
                return;
            }

            if (!entityManager.TryGetEntity(partUid, out var partEntity))
            {
                shell.WriteLine($"{partUid} is not a valid entity.");
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(partEntity, out SharedBodyPartComponent? part))
            {
                shell.WriteLine($"Entity {IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(partEntity).EntityName} with uid {args[0]} does not have a {nameof(SharedBodyPartComponent)} component.");
                return;
            }

            if (body.HasPart(part))
            {
                shell.WriteLine($"Body part {IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(partEntity).EntityName} with uid {partEntity} is already attached to entity {IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityName} with uid {entity}");
                return;
            }

            body.SetPart($"AttachBodyPartVerb-{partEntity}", part);
        }
    }
}
