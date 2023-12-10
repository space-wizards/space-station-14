using Robust.Shared.Random;

namespace Content.Server.Procedural;

public sealed class RoomFillSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoomFillComponent, MapInitEvent>(OnRoomFillMapInit);
    }

    private void OnRoomFillMapInit(Entity<RoomFillComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent.Owner);

        if (xform.GridUid != null)
        {
            var room = _random.Pick(ent.Comp.RoomPrototypes);


        }

        // Final cleanup
        QueueDel(ent.Owner);
    }
}
