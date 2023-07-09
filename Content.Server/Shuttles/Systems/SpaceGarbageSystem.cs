using Content.Server.Shuttles.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;

namespace Content.Server.Shuttles.Systems;

/// <summary>
///     Deletes anything with <see cref="SpaceGarbageComponent"/> that has a cross-grid collision with a static body.
/// </summary>
public sealed class SpaceGarbageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaceGarbageComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(EntityUid uid, SpaceGarbageComponent component, ref StartCollideEvent args)
    {
        if (args.OtherBody.BodyType != BodyType.Static) return;

        var ourXform = Transform(uid);
        var otherXform = Transform(args.OtherEntity);

        if (ourXform.GridUid == otherXform.GridUid) return;

        QueueDel(uid);
    }
}
