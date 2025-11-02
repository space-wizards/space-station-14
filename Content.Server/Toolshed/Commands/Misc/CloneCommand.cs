using Content.Server.Administration;
using Content.Server.Humanoid;
using Content.Shared.Administration;
using Content.Shared.Cloning;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Cloning.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class CloneCommand : ToolshedCommand
{
    private HumanoidAppearanceSystem? _appearance;
    private CloningSystem? _cloning;
    private MetaDataSystem? _metadata;

    [CommandImplementation("humanoidappearance")]
    public IEnumerable<EntityUid> HumanoidAppearance([PipedArgument] IEnumerable<EntityUid> targets, EntityUid source, bool rename)
    {
        _appearance ??= GetSys<HumanoidAppearanceSystem>();
        _metadata ??= GetSys<MetaDataSystem>();

        foreach (var ent in targets)
        {
            _appearance.CloneAppearance(source, ent);

            if (rename)
                _metadata.SetEntityName(ent, MetaData(source).EntityName, raiseEvents: true);

            yield return ent;
        }
    }

    [CommandImplementation("comps")]
    public IEnumerable<EntityUid> Comps([PipedArgument] IEnumerable<EntityUid> targets, EntityUid source, ProtoId<CloningSettingsPrototype> settings)
    {
        _cloning ??= GetSys<CloningSystem>();

        foreach (var ent in targets)
        {
            _cloning.CloneComponents(source, ent, settings);
            yield return ent;
        }
    }

    [CommandImplementation("equipment")]
    public IEnumerable<EntityUid> Equipment([PipedArgument] IEnumerable<EntityUid> targets, EntityUid source, SlotFlags flags)
    {
        _cloning ??= GetSys<CloningSystem>();

        foreach (var ent in targets)
        {
            _cloning.CopyEquipment(source, ent, flags);
            yield return ent;
        }
    }

    [CommandImplementation("implants")]
    public IEnumerable<EntityUid> Implants([PipedArgument] IEnumerable<EntityUid> targets, EntityUid source, bool copyStorage)
    {
        _cloning ??= GetSys<CloningSystem>();

        foreach (var ent in targets)
        {
            _cloning.CopyImplants(source, ent, copyStorage);
            yield return ent;
        }
    }

    [CommandImplementation("storage")]
    public IEnumerable<EntityUid> InternalStorage([PipedArgument] IEnumerable<EntityUid> targets, EntityUid source)
    {
        _cloning ??= GetSys<CloningSystem>();

        foreach (var ent in targets)
        {
            _cloning.CopyStorage(source, ent);
            yield return ent;
        }
    }
}
