using Content.Server.Access.Systems;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Server.Mind.Components;
using Content.Server.PDA;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;

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
        if (name.Length > IdCardConsoleComponent.MaxFullNameLength)
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

        if (entMan.TryGetComponent(entityUid, out MindComponent? mind) && mind.Mind != null)
        {
            // Mind
            mind.Mind.CharacterName = name;
        }

        // Id Cards
        if (entSysMan.TryGetEntitySystem<IdCardSystem>(out var idCardSystem))
        {
            if (idCardSystem.TryFindIdCard(entityUid, out var idCard))
            {
                idCardSystem.TryChangeFullName(idCard.Owner, name, idCard);

                // Records
                // This is done here because ID cards are linked to station records
                if (entSysMan.TryGetEntitySystem<StationRecordsSystem>(out var recordsSystem)
                    && entMan.TryGetComponent(idCard.Owner, out StationRecordKeyStorageComponent? keyStorage)
                    && keyStorage.Key != null)
                {
                    if (recordsSystem.TryGetRecord<GeneralStationRecord>(keyStorage.Key.Value.OriginStation,
                            keyStorage.Key.Value,
                            out var generalRecord))
                    {
                        generalRecord.Name = name;
                    }

                    recordsSystem.Synchronize(keyStorage.Key.Value.OriginStation);
                }
            }
        }

        // PDAs
        if (entSysMan.TryGetEntitySystem<PDASystem>(out var pdaSystem))
        {
            var query = entMan.EntityQueryEnumerator<PDAComponent>();
            while (query.MoveNext(out var uid, out var pda))
            {
                if (pda.OwnerName == oldName)
                {
                    pdaSystem.SetOwner(uid, pda, name);
                }
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
