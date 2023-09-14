using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class AttachBodyPartCommand : IConsoleCommand
    {
        public string Command => "attachbodypart";
        public string Description => "Attaches a body part to you or someone else.";
        public string Help => $"{Command} <partEntityUid> / {Command} <entityUid> <partEntityUid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var entityManager = IoCManager.Resolve<IEntityManager>();

            EntityUid bodyId;
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

                    bodyId = player.AttachedEntity.Value;

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

                    if (!entityManager.EntityExists(entityUid))
                    {
                        shell.WriteLine($"{entityUid} is not a valid entity.");
                        return;
                    }

                    bodyId = entityUid;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (!entityManager.TryGetComponent(bodyId, out BodyComponent? body))
            {
                shell.WriteLine($"Entity {entityManager.GetComponent<MetaDataComponent>(bodyId).EntityName} with uid {bodyId} does not have a {nameof(BodyComponent)}.");
                return;
            }

            if (!entityManager.EntityExists(partUid))
            {
                shell.WriteLine($"{partUid} is not a valid entity.");
                return;
            }

            if (!entityManager.TryGetComponent(partUid, out BodyPartComponent? part))
            {
                shell.WriteLine($"Entity {entityManager.GetComponent<MetaDataComponent>(partUid).EntityName} with uid {args[0]} does not have a {nameof(BodyPartComponent)}.");
                return;
            }

            var bodySystem = entityManager.System<BodySystem>();
            if (bodySystem.BodyHasChild(bodyId, partUid, body, part))
            {
                shell.WriteLine($"Body part {entityManager.GetComponent<MetaDataComponent>(partUid).EntityName} with uid {partUid} is already attached to entity {entityManager.GetComponent<MetaDataComponent>(bodyId).EntityName} with uid {bodyId}");
                return;
            }

            var slotId = $"AttachBodyPartVerb-{partUid}";

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (bodySystem.TryCreateBodyRootSlot(bodyId, slotId, out var rootSlot, body))
            {
                bodySystem.DropPart(partUid, part);
                bodySystem.AttachPart(partUid, rootSlot, part);
            }
            else
            {
                var attachAt = bodySystem.GetBodyChildren(bodyId, body).First();

                if (!bodySystem.TryCreatePartSlotAndAttach(attachAt.Id, slotId, partUid, attachAt.Component, part))
                {
                    shell.WriteError($"Could not create slot {slotId} on entity {entityManager.ToPrettyString(bodyId)}");
                    return;
                }
            }

            shell.WriteLine($"Attached part {entityManager.ToPrettyString(partUid)} to {entityManager.ToPrettyString(bodyId)}");
        }
    }
}
