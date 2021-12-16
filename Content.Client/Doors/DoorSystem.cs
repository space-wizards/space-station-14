using Content.Shared.Doors;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Doors.DoorComponent;

namespace Content.Client.Doors;

public sealed class DoorSystem : SharedDoorSystem
{
    // Gotta love it when both the client-side and server-side sprite components both have a draw depth, but for whatever bloody
    // reason the shared component doesn't.
    protected override void UpdateAppearance(EntityUid uid, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref door))
            return;

        base.UpdateAppearance(uid, door);

        if (TryComp(uid, out SpriteComponent? sprite))
        {
            sprite.DrawDepth = (door.State == DoorState.Open)
                ? door.OpenDrawDepth
                : door.ClosedDrawDepth;
        }
    }
}
