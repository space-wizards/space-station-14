using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.Console;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class AttachBodyPartCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override string Command => "attachbodypart";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;

            EntityUid bodyId;
            EntityUid? partUid;

            switch (args.Length)
            {
                case 1:
                    if (player == null)
                    {
                        shell.WriteLine(Loc.GetString("cmd-attachbodypart-only-player-run-without-args"));
                        shell.WriteLine(Help);
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.WriteLine(Loc.GetString("cmd-attachbodypart-no-entity"));
                        shell.WriteLine(Help);
                        return;
                    }

                    if (!NetEntity.TryParse(args[0], out var partNet) || !EntityManager.TryGetEntity(partNet, out partUid))
                    {
                        shell.WriteLine(Loc.GetString("cmd-attachbodypart-invalid-entity-uid", ("uid", args[0])));
                        return;
                    }

                    bodyId = player.AttachedEntity.Value;

                    break;
                case 2:
                    if (!NetEntity.TryParse(args[0], out var entityNet) || !EntityManager.TryGetEntity(entityNet, out var entityUid))
                    {
                        shell.WriteLine(Loc.GetString("cmd-attachbodypart-invalid-entity-uid", ("uid", args[0])));
                        return;
                    }

                    if (!NetEntity.TryParse(args[1], out partNet) || !EntityManager.TryGetEntity(partNet, out partUid))
                    {
                        shell.WriteLine(Loc.GetString("cmd-attachbodypart-invalid-entity-uid", ("uid", args[1])));
                        return;
                    }

                    if (!EntityManager.EntityExists(entityUid))
                    {
                        shell.WriteLine(Loc.GetString("cmd-attachbodypart-invalid-entity", ("uid", entityUid)));
                        return;
                    }

                    bodyId = entityUid.Value;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (!EntityManager.TryGetComponent(bodyId, out BodyComponent? body))
            {
                shell.WriteLine(Loc.GetString("cmd-attachbodypart-entity-no-body-component", ("entity", EntityManager.GetComponent<MetaDataComponent>(bodyId).EntityName), ("component", nameof(BodyComponent))));
                return;
            }

            if (!EntityManager.EntityExists(partUid))
            {
                shell.WriteLine(Loc.GetString("cmd-attachbodypart-invalid-entity", ("uid", partUid)));
                return;
            }

            if (!EntityManager.TryGetComponent(partUid, out BodyPartComponent? part))
            {
                shell.WriteLine(Loc.GetString("cmd-attachbodypart-entity-no-body-part-component", ("entity", EntityManager.GetComponent<MetaDataComponent>(partUid.Value).EntityName), ("component", nameof(BodyPartComponent))));
                return;
            }

            if (_bodySystem.BodyHasChild(bodyId, partUid.Value, body, part))
            {
                shell.WriteLine(Loc.GetString("cmd-attachbodypart-body-part-already-attached", ("entity", EntityManager.GetComponent<MetaDataComponent>(partUid.Value).EntityName), ("uid", partUid), ("bodyId", bodyId)));
                return;
            }

            var slotId = $"AttachBodyPartVerb-{partUid}";

            if (body.RootContainer.ContainedEntity is null && !_bodySystem.AttachPartToRoot(bodyId, partUid.Value, body, part))
            {
                shell.WriteError(Loc.GetString("cmd-attachbodypart-body-container-no-root-entity"));
                return;
            }

            var (rootPartId, rootPart) = _bodySystem.GetRootPartOrNull(bodyId, body)!.Value;
            if (!_bodySystem.TryCreatePartSlotAndAttach(rootPartId,
                    slotId,
                    partUid.Value,
                    part.PartType,
                    rootPart,
                    part))
            {
                shell.WriteError(Loc.GetString("cmd-attachbodypart-could-not-create-slot", ("slotId", slotId), ("entity", EntityManager.ToPrettyString(bodyId))));
                return;
            }
            shell.WriteLine(Loc.GetString("cmd-attachbodypart-attached-part", ("part", EntityManager.ToPrettyString(partUid.Value)), ("entity", EntityManager.ToPrettyString(bodyId))));
        }
    }
}
