using Content.Server.Speech.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class OwoifyCommand : ToolshedCommand
{
    private MetaDataSystem? _metaSystem;
    private OwOAccentSystem? _owoSystem;

    [CommandImplementation]
    public void Owoify(EntityUid entity)
    {
        var meta = Comp<MetaDataComponent>(entity);
        _owoSystem ??=GetSys<OwOAccentSystem>();
        _metaSystem ??= GetSys<MetaDataSystem>();
        _metaSystem.SetEntityName(entity, _owoSystem.Accentuate(meta.EntityName), meta);
        _metaSystem.SetEntityDescription(entity, _owoSystem.Accentuate(meta.EntityDescription), meta);
    }

    [CommandImplementation]
    public void Owoify([PipedArgument] IEnumerable<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            Owoify(entity);
        }
    }
}
