using Content.Server.Shuttles.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

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

    private void OnCollide(EntityUid uid, SpaceGarbageComponent component, StartCollideEvent args)
    {
        var ourXform = Transform(args.OurFixture.Body.Owner);
        var otherXform = Transform(args.OtherFixture.Body.Owner);

        if (ourXform.GridEntityId == otherXform.GridEntityId ||
            args.OtherFixture.Body.BodyType != BodyType.Static) return;

        QueueDel(uid);
    }
}
