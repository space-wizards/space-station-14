using Content.Shared.Security.Components;
using Content.Shared.Security.Systems;

namespace Content.Server.Security;

public sealed class GenpopSystem : SharedGenpopSystem
{
    protected override void CreateId(Entity<GenpopLockerComponent> ent, string name, float sentence, string crime)
    {
        var xform = Transform(ent);
        var uid = Spawn(ent.Comp.IdCardProto, xform.Coordinates);
        ent.Comp.LinkedId = uid;
        IdCard.TryChangeFullName(uid, name);

        if (TryComp<GenpopIdCardComponent>(uid, out var id))
        {
            id.Crime = crime;
            id.SentenceDuration = TimeSpan.FromMinutes(sentence);
            Dirty(uid, id);
        }
        if (sentence <= 0)
            IdCard.SetPermanent(uid, true);
        IdCard.SetExpireTime(uid, TimeSpan.FromMinutes(sentence) + Timing.CurTime);

        var metaData = MetaData(ent);
        MetaDataSystem.SetEntityName(ent, Loc.GetString("genpop-locker-name-used", ("name", name)), metaData);
        MetaDataSystem.SetEntityDescription(ent, Loc.GetString("genpop-locker-desc-used", ("name", name)), metaData);
        Dirty(ent);
    }
}
