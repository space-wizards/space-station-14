using System.Numerics;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Random;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Traits;

public sealed partial class ParacusiaSystem : SharedParacusiaSystem
{
    private static readonly EntityTimerId IncidentTimer = new("incident");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParacusiaComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ParacusiaComponent, LocalPlayerDetachedEvent>(OnPlayerDetach);
        SubscribeLocalEvent<ParacusiaComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnComponentStartup(EntityUid uid, ParacusiaComponent component, ComponentStartup args)
    {
        component.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents));
        _timers.SetTimerAt<ParacusiaComponent>((uid, component), IncidentTimer, component.NextIncidentTime);
    }

    private void OnPlayerDetach(EntityUid uid, ParacusiaComponent component, LocalPlayerDetachedEvent args)
    {
        component.Stream = _audio.Stop(component.Stream);
    }

    private void OnTimer(Entity<ParacusiaComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != IncidentTimer)
            return;

        var paracusia = ent.Comp;

        var timeInterval = _random.NextFloat(paracusia.MinTimeBetweenIncidents, paracusia.MaxTimeBetweenIncidents);
        paracusia.NextIncidentTime = args.ScheduledTime + TimeSpan.FromSeconds(timeInterval);
        _timers.SetTimerAt(ent, IncidentTimer, paracusia.NextIncidentTime);

        if (!_timing.IsFirstTimePredicted || _player.LocalEntity != ent.Owner)
            return;

        // Offset position where the sound is played
        var randomOffset =
            new Vector2
            (
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance),
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance)
            );

        var newCoords = Transform(ent).Coordinates.Offset(randomOffset);

        // Play the sound
        paracusia.Stream = _audio.PlayStatic(paracusia.Sounds, ent, newCoords)?.Entity;
    }

}
