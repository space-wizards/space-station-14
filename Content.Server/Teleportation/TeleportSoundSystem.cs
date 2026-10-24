using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Teleportation;

/// <summary>
/// Plays the teleporter's sounds at the target's departure and arrival locations.
/// </summary>
public sealed partial class TeleportSoundSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportSoundComponent, BeforeTeleportEvent>(OnBeforeTeleport);
        SubscribeLocalEvent<TeleportSoundComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    /// <summary>
    /// Plays the departure sound at fixed coordinates so it stays at the source after the target teleports.
    /// </summary>
    private void OnBeforeTeleport(Entity<TeleportSoundComponent> ent, ref BeforeTeleportEvent args)
    {
        var sound = ent.Comp.DepartureSound;
        if (sound == null)
            return;

        _audio.PlayPvs(sound, Transform(args.Target).Coordinates);
    }

    /// <summary>
    /// Plays the arrival sound at fixed coordinates without attaching it to the target.
    /// </summary>
    private void OnTargetTeleported(Entity<TeleportSoundComponent> ent, ref TargetTeleportedEvent args)
    {
        var sound = ent.Comp.ArrivalSound;
        if (sound == null)
            return;

        _audio.PlayPvs(sound, Transform(args.Target).Coordinates);
    }
}
