using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Access.Systems;
using Content.Server.Administration;
using Content.Server.Cloning;
using Content.Server.Mind.Components;
using Content.Server.PDA;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class RenameCommand : IConsoleCommand
{
    public string Command => "rename";
    public string Description => "Renames an entity and its cloner entries, ID cards, and PDAs.";
    public string Help => "rename <Username|EntityUid> <New character name>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        var name = args[1];
        if (name.Length > SharedIdCardConsoleComponent.MaxFullNameLength)
        {
            shell.WriteLine("Name is too long.");
            return;
        }

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!TryParseUid(args[0], shell, entMan, out var entityUid))
            return;

        // Metadata
        var metadata = entMan.GetComponent<MetaDataComponent>(entityUid);
        var oldName = metadata.EntityName;
        metadata.EntityName = name;

        var entSysMan = IoCManager.Resolve<IEntitySystemManager>();

        if (entMan.TryGetComponent(entityUid, out MindComponent mind) && mind.Mind != null)
        {
            // Mind
            mind.Mind.CharacterName = name;

            // Cloner entries
            if (entSysMan.TryGetEntitySystem<CloningSystem>(out var cloningSystem)
                && cloningSystem.MindToId.TryGetValue(mind.Mind, out var cloningId)
                && cloningSystem.IdToDNA.ContainsKey(cloningId))
            {
                cloningSystem.IdToDNA[cloningId] =
                    new ClonerDNAEntry(mind.Mind, cloningSystem.IdToDNA[cloningId].Profile.WithName(name));
            }
        }

        // Id Cards
        if (entSysMan.TryGetEntitySystem<IdCardSystem>(out var idCardSystem))
        {
            if (idCardSystem.TryFindIdCard(entityUid, out var idCard))
                idCardSystem.TryChangeFullName(idCard.Owner, name, idCard);
            else
            {
                foreach (var idCardComponent in entMan.EntityQuery<IdCardComponent>())
                {
                    if (idCardComponent.OriginalOwnerName != oldName)
                        continue;
                    idCardSystem.TryChangeFullName(idCardComponent.Owner, name, idCardComponent);
                }
            }
        }

        // PDAs
        if (entSysMan.TryGetEntitySystem<PDASystem>(out var pdaSystem))
        {
            foreach (var pdaComponent in entMan.EntityQuery<PDAComponent>())
            {
                if (pdaComponent.OwnerName != oldName)
                    continue;
                pdaSystem.SetOwner(pdaComponent, name);
            }
        }

        // Admin Overlay
        if (entSysMan.TryGetEntitySystem<AdminSystem>(out var adminSystem)
            && entMan.TryGetComponent<ActorComponent>(entityUid, out var actorComp))
        {
            adminSystem.UpdatePlayerList(actorComp.PlayerSession);
        }
    }

    private static bool TryParseUid(string str, IConsoleShell shell,
        IEntityManager entMan, out EntityUid entityUid)
    {
        if (EntityUid.TryParse(str, out entityUid) && entMan.EntityExists(entityUid))
            return true;

        var playerMan = IoCManager.Resolve<IPlayerManager>();
        if (playerMan.TryGetSessionByUsername(str, out var session) && session.AttachedEntity.HasValue)
        {
            entityUid = session.AttachedEntity.Value;
            return true;
        }

        if (session == null)
            shell.WriteError("Can't find username/uid: " + str);
        else
            shell.WriteError(str + " does not have an entity.");
        return false;
    }
}
