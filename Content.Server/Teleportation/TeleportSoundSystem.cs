using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Teleportation;

public sealed partial class TeleportSoundSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportSoundComponent, BeforeTeleportEvent>(OnBeforeTeleport);
        SubscribeLocalEvent<TeleportSoundComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnBeforeTeleport(Entity<TeleportSoundComponent> ent, ref BeforeTeleportEvent args)
    {
        var sound = ent.Comp.DepartureSound;
        if (sound == null)
            return;

        // Keep the sound at the departure location instead of letting it follow the teleported target.
        _audio.PlayPvs(sound, Transform(args.Target).Coordinates);
    }

    private void OnTargetTeleported(Entity<TeleportSoundComponent> ent, ref TargetTeleportedEvent args)
    {
        var sound = ent.Comp.ArrivalSound;
        if (sound == null)
            return;

        // Keep the sound at the arrival location if the target moves away after teleporting.
        _audio.PlayPvs(sound, Transform(args.Target).Coordinates);
    }
}
