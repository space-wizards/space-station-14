using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Plays the teleporter's sounds at the target's departure and arrival locations.
/// </summary>
public sealed partial class TeleportSoundSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// Plays the departure sound at fixed coordinates so it stays at the source after the target teleports.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeforeTeleport(Entity<TeleportSoundComponent> ent, ref BeforeTeleportEvent args)
    {
        var sound = ent.Comp.DepartureSound;
        if (sound == null)
            return;

        _audio.PlayPredicted(sound, Transform(args.Target).Coordinates, ent.Owner);
    }

    /// <summary>
    /// Plays the arrival sound at fixed coordinates without attaching it to the target.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTargetTeleported(Entity<TeleportSoundComponent> ent, ref TargetTeleportedEvent args)
    {
        var sound = ent.Comp.ArrivalSound;
        if (sound == null)
            return;

        _audio.PlayPredicted(sound, Transform(args.Target).Coordinates, ent.Owner);
    }
}
