using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client.Doors;

public sealed class DoorSystem : SharedDoorSystem
{
    // Gotta love it when both the client-side and server-side sprite components both have a draw depth, but for
    // whatever bloody reason the shared component doesn't.
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

    // TODO AUDIO PREDICT see comments in server-side PlaySound()
    protected override void PlaySound(EntityUid uid, SoundSpecifier soundSpecifier, AudioParams audioParams, EntityUid? predictingPlayer, bool predicted)
    {
        if (GameTiming.InPrediction && GameTiming.IsFirstTimePredicted)
            Audio.Play(soundSpecifier, Filter.Local(), uid, false, audioParams);
    }
}
