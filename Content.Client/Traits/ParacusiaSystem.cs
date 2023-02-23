using Content.Shared.Traits.Assorted;
using Content.Client.Camera;
using Robust.Shared.Random;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Traits;

public sealed class ParacusiaSystem : SharedParacusiaSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CameraRecoilSystem _camera = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParacusiaComponent, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<ParacusiaComponent, PlayerDetachedEvent>(OnPlayerDetach);
    }

    private void OnPlayerDetach(EntityUid uid, ParacusiaComponent component, PlayerDetachedEvent args)
    {
        component.Stream?.Stop();
    }

    private void OnCompStartup(EntityUid uid, ParacusiaComponent component, ComponentStartup args)
    {
        component.NextIncidentTime += TimeSpan.FromSeconds(_random.NextFloat(component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var localPlayer = _player.LocalPlayer?.ControlledEntity;

        if (!TryComp<ParacusiaComponent>(localPlayer, out var paracusia))
            return;

        var curTime = _timing.CurTime;

        if (curTime < paracusia.NextIncidentTime)
            return;

        // Set the new time.
        paracusia.NextIncidentTime =+ TimeSpan.FromSeconds(_random.NextFloat(paracusia.MinTimeBetweenIncidents, paracusia.MaxTimeBetweenIncidents));

        // Offset position where the sound is played
        var randomOffset =
            new Vector2
            (
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance),
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance)
            );

        var newCoords = Transform(localPlayer.Value).Coordinates
            .Offset(randomOffset);

        // Play the sound
        paracusia.Stream = _audio.PlayStatic(paracusia.Sounds, localPlayer.Value, newCoords);
    }
}
