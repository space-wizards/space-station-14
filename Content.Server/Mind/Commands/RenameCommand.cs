using System.Collections.Generic;
using Content.Server.Access.Systems;
using Content.Server.Administration;
using Content.Server.Cloning;
using Content.Server.Mind.Components;
using Content.Server.PDA;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.PDA;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public class RenameCommand : IConsoleCommand
{
    public string Command => "rename";
    public string Description => "Renames an entity and its cloner entries, ID cards, and PDAs.";
    public string Help => "rename <EntityUid> <Character Name>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entityUid))
        {
            shell.WriteLine("Invalid argument.");
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();

        if (!entityManager.EntityExists(entityUid))
        {
            shell.WriteLine("Invalid entity specified!");
            return;
        }

        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();

        var metadata = entityManager.GetComponent<MetaDataComponent>(entityUid);

        var oldName = metadata.EntityName;
        var name = string.Join(" ", args[1..]);
        if (name.Length > SharedIdCardConsoleComponent.MaxFullNameLength)
        {
            shell.WriteLine("Name is too long.");
            return;
        }

        // Metadata
        metadata.EntityName = name;

        if (entityManager.TryGetComponent(entityUid, out MindComponent mind) && mind.Mind != null)
        {
            // Mind
            mind.Mind.CharacterName = name;

            // Cloner entries
            if (entitySystemManager.TryGetEntitySystem<CloningSystem>(out var cloningSystem)
                && cloningSystem.MindToId.TryGetValue(mind.Mind, out var cloningId)
                && cloningSystem.IdToDNA.ContainsKey(cloningId))
            {
                cloningSystem.IdToDNA[cloningId] =
                    new ClonerDNAEntry(mind.Mind, cloningSystem.IdToDNA[cloningId].Profile.WithName(name));
            }
        }

        // Id Cards
        if (entitySystemManager.TryGetEntitySystem<IdCardSystem>(out var idCardSystem))
        {
            if (idCardSystem.TryFindIdCard(entityUid, out var idCard))
                idCardSystem.TryChangeFullName(idCard.Owner, name, idCard);
            else
            {
                foreach (var idCardComponent in entityManager.EntityQuery<IdCardComponent>())
                {
                    if (idCardComponent.OriginalOwnerName != oldName)
                        continue;
                    idCardSystem.TryChangeFullName(idCardComponent.Owner, name, idCardComponent);
                }
            }
        }

        // PDAs
        if (entitySystemManager.TryGetEntitySystem<PDASystem>(out var pdaSystem))
        {
            foreach (var pdaComponent in entityManager.EntityQuery<PDAComponent>())
            {
                if (pdaComponent.OwnerName != oldName)
                    continue;
                pdaSystem.SetOwner(pdaComponent, name);
            }
        }
    }
}
