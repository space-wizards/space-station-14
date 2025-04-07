using Content.Server.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Lock;
using Content.Shared.Security.Components;
using Content.Shared.Security.Systems;
using Content.Shared.Storage.Components;

namespace Content.Server.Security;

public sealed class GenpopSystem : SharedGenpopSystem
{
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly IdCardSystem _id = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GenpopLockerComponent, StorageAfterCloseEvent>(OnAfterClose);
    }

    private void OnAfterClose(Entity<GenpopLockerComponent> ent, ref StorageAfterCloseEvent args)
    {
        if (ent.Comp.LinkedId == null)
        {
            // TODO: instead of creating an ID, attempting to close a door should bring up the ui.
            // after filling out the ui, then we should close the UI.

            CreateId(ent);
            // Automatically lock inside belongings.
            _lock.Lock(ent.Owner, null);
        }
    }

    private void CreateId(Entity<GenpopLockerComponent> ent)
    {
        // TODO: do properly.
        var name = "DEFAULT NAME";
        var crime = "[REDACTED]";
        var sentence = TimeSpan.FromSeconds(30);

        var xform = Transform(ent);
        var uid = Spawn(ent.Comp.IdCardProto, xform.Coordinates);
        ent.Comp.LinkedId = uid;
        _id.TryChangeFullName(uid, name);

        _id.SetExpireTime(uid, Timing.CurTime + sentence);
        if (TryComp<GenpopIdCardComponent>(uid, out var id))
        {
            id.Crime = crime;
            id.StartTime = Timing.CurTime;
            Dirty(uid, id);
        }

        var metaData = MetaData(ent);
        MetaDataSystem.SetEntityName(ent, Loc.GetString("genpop-locker-name-used", ("name", name)), metaData);
        MetaDataSystem.SetEntityDescription(ent, Loc.GetString("genpop-locker-desc-used", ("name", name)), metaData);
        Dirty(ent);
    }
}
