using Content.Shared.Audio;
using Content.Shared.Traits.Assorted;
using Content.Client.Camera;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Content.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Serilog.Debugging;
using Content.Shared.GameTicking;

namespace Content.Client.Traits;

public sealed class ParacusiaSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CameraRecoilSystem _camera = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ParacusiaComponent, ComponentStartup>(SetupParacusia);
        SubscribeLocalEvent<ParacusiaComponent, RoundRestartCleanupEvent>(ShutdownParacusia);
    }

    private void SetupParacusia(EntityUid uid, ParacusiaComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents);
    }

    public void ShutdownParacusia(EntityUid uid, ParacusiaComponent component, RoundRestartCleanupEvent ev)
    {
        component.Stream?.Stop();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var paracusia in EntityQuery<ParacusiaComponent>())
        {
            paracusia.NextIncidentTime -= frameTime;

            if (paracusia.NextIncidentTime >= 0)
                continue;

            // Set the new time.
            paracusia.NextIncidentTime +=
                _random.NextFloat(paracusia.MinTimeBetweenIncidents, paracusia.MaxTimeBetweenIncidents);

            // Offset position where the sound is played
            Vector2 randomOffset =
            new Vector2
            (
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance),
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance)
            );

            EntityCoordinates newCoords = Transform(paracusia.Owner).Coordinates
                .Offset(randomOffset);

            // funy camera shake
            _camera.KickCamera(paracusia.Owner, randomOffset);

            // Play the sound
            paracusia.Stream = _audio.PlayStatic(paracusia.Sounds, paracusia.Owner, newCoords);
        }
    }
}
