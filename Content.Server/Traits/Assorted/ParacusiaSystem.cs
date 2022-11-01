using Content.Shared.Audio;
using Content.Server.Camera;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Map;

namespace Content.Server.Traits.Assorted;

public sealed class ParacusiaSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CameraRecoilSystem _camera = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ParacusiaComponent, ComponentStartup>(SetupParacusia);
    }

    private void SetupParacusia(EntityUid uid, ParacusiaComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
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
                _random.NextFloat(paracusia.TimeBetweenIncidents.X, paracusia.TimeBetweenIncidents.Y);

            List<string> sounds = paracusia.Sounds ?? new List<string> { };
            if (paracusia.Sounds == null || paracusia.Sounds.Count == 0)
                return;

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
            int randomIndex = _random.Next(0, paracusia.Sounds.Count);
            _audio.Play(paracusia.Sounds[randomIndex], Filter.Entities(paracusia.Owner), newCoords);
        }
    }
}
