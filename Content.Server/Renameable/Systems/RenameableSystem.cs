using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Renameable;
using Content.Shared.Renameable.Components;
using Content.Shared.Renameable.Systems;
using Robust.Server.GameObjects;

public sealed class RenameableSystem : SharedRenameableSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenameableComponent, RenamedMessage>(OnRenamed);
    }

    private void OnRenamed(EntityUid uid, RenameableComponent comp, RenamedMessage args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        var user = args.Session.AttachedEntity.Value;
        // perform validation on the string
        var name = args.Name.Trim();
        if (name.Length > comp.MaxLength)
            name = name.Substring(0, comp.MaxLength);
        if (comp.Suffix != null)
            SetSuffix(ref name, comp.Suffix);

        // if name wasn't actually changed ignore it
        var meta = MetaData(uid);
        var oldName = meta.EntityName;
        if (name == oldName)
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} renamed {ToPrettyString(uid):target} to '{name}'");
        _metaData.SetEntityName(uid, name, meta);
    }

    private void SetSuffix(ref string name, string suffix)
    {
        // already has the suffix so do nothinga
        if (name.EndsWith(suffix))
            return;

        // add the suffix
        name = $"{name} {suffix}";
    }

    protected override bool TryOpen(EntityUid uid, EntityUid user)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        return _ui.TryOpen(uid, RenamingUiKey.Key, actor.PlayerSession);
    }
}
