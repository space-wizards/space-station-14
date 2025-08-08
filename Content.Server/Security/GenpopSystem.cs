using Content.Shared.Security.Components;
using Content.Shared.Security.Systems;
using Content.Shared.Wall;

namespace Content.Server.Security;

public sealed class GenpopSystem : SharedGenpopSystem
{
    private const float GenpopIDEjectDistanceFromWall = 1f;
    protected override void CreateId(Entity<GenpopLockerComponent> ent, string name, float sentence, string crime)
    {
        // Default to prisoner locker coordinates for ID spawn
        var xform = Transform(ent);
        var spawnCoordinates = xform.Coordinates;
        // Offset prisoner wall locker coordinates in wallmount direction for ID spawn; avoids spawning ID inside wall
        if (TryComp<WallMountComponent>(ent, out var wallMountComponent))
        {
            var offset = (wallMountComponent.Direction + xform.LocalRotation - Math.PI / 2).ToVec() * GenpopIDEjectDistanceFromWall;
            spawnCoordinates = spawnCoordinates.Offset(offset);
        }
        var uid = Spawn(ent.Comp.IdCardProto, spawnCoordinates);
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
