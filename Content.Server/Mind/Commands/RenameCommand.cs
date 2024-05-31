using System.Diagnostics.CodeAnalysis;
using Content.Server.Access.Systems;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Server.PDA;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class RenameCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

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

        if (!TryParseUid(args[0], shell, _entManager, out var entityUid))
            return;

        // Metadata
        var metadata = _entManager.GetComponent<MetaDataComponent>(entityUid.Value);
        var oldName = metadata.EntityName;
        _entManager.System<MetaDataSystem>().SetEntityName(entityUid.Value, name, metadata);

        var minds = _entManager.System<SharedMindSystem>();

        if (minds.TryGetMind(entityUid.Value, out var mindId, out var mind))
        {
            // Mind
            mind.CharacterName = name;
            _entManager.Dirty(mindId, mind);
        }

        // Id Cards
        if (_entManager.TrySystem<IdCardSystem>(out var idCardSystem))
        {
            if (idCardSystem.TryFindIdCard(entityUid.Value, out var idCard))
            {
                idCardSystem.TryChangeFullName(idCard, name, idCard);

                // Records
                // This is done here because ID cards are linked to station records
                if (_entManager.TrySystem<StationRecordsSystem>(out var recordsSystem)
                    && _entManager.TryGetComponent(idCard, out StationRecordKeyStorageComponent? keyStorage)
                    && keyStorage.Key is {} key)
                {
                    if (recordsSystem.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
                    {
                        generalRecord.Name = name;
                    }

                    recordsSystem.Synchronize(key);
                }
            }
        }

        // PDAs
        if (_entManager.TrySystem<PdaSystem>(out var pdaSystem))
        {
            var query = _entManager.EntityQueryEnumerator<PdaComponent>();
            while (query.MoveNext(out var uid, out var pda))
            {
                if (pda.OwnerName == oldName)
                {
                    pdaSystem.SetOwner(uid, pda, name);
                }
            }
        }

        // Admin Overlay
        if (_entManager.TrySystem<AdminSystem>(out var adminSystem)
            && _entManager.TryGetComponent<ActorComponent>(entityUid, out var actorComp))
        {
            adminSystem.UpdatePlayerList(actorComp.PlayerSession);
        }
    }

    private bool TryParseUid(string str, IConsoleShell shell,
        IEntityManager entMan, [NotNullWhen(true)] out EntityUid? entityUid)
    {
        if (NetEntity.TryParse(str, out var entityUidNet) && _entManager.TryGetEntity(entityUidNet, out entityUid) && entMan.EntityExists(entityUid))
            return true;

        if (_playerManager.TryGetSessionByUsername(str, out var session) && session.AttachedEntity.HasValue)
        {
            entityUid = session.AttachedEntity.Value;
            return true;
        }

        if (session == null)
            shell.WriteError("Can't find username/uid: " + str);
        else
            shell.WriteError(str + " does not have an entity.");

        entityUid = EntityUid.Invalid;
        return false;
    }
}
