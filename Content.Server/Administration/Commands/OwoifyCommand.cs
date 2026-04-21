using Content.Server.Speech.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class OwoifyCommand : ToolshedCommand
{
    private OwOAccentSystem? _owoSystem;
    private MetaDataSystem? _metaSystem;

    [CommandImplementation]
    public void Owoify(EntityUid entityUid)
    {
        var meta = Comp<MetaDataComponent>(entityUid);

        _owoSystem ??= GetSys<OwOAccentSystem>();
        _metaSystem ??= GetSys<MetaDataSystem>();

        _metaSystem.SetEntityName(entityUid, _owoSystem.Accentuate(meta.EntityName), meta);
        _metaSystem.SetEntityDescription(entityUid, _owoSystem.Accentuate(meta.EntityDescription), meta);
    }

    [CommandImplementation]
    public void Owoify([PipedArgument] IEnumerable<EntityUid> entityUids)
    {
        foreach (var entityUid in entityUids)
        {
            Owoify(entityUid);
        }
    }
}
