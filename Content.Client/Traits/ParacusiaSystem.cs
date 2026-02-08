using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Traits.Assorted;
using Robust.Client.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Traits;

public sealed class ParacusiaSystem : SharedParacusiaSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlock = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParacusiaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ParacusiaComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ParacusiaComponent, LocalPlayerDetachedEvent>(OnPlayerDetach);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } localPlayer
            || IsPaused(localPlayer))
            return;

        PlayParacusiaSounds(localPlayer);
    }


    private void OnMapInit(Entity<ParacusiaComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime;
        SetNewIncidentTime(ent);
    }

    private void OnPlayerAttached(Entity<ParacusiaComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime;
        SetNewIncidentTime(ent);
    }

    private void OnPlayerDetach(Entity<ParacusiaComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        ent.Comp.Stream = _audio.Stop(ent.Comp.Stream);
    }

    private void PlayParacusiaSounds(Entity<ParacusiaComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (_timing.CurTime <= ent.Comp.NextIncidentTime)
            return;

        SetNewIncidentTime((ent, ent.Comp));

        // Would we actually have heard this?
        if (!_actionBlock.CanConsciouslyPerformAction(ent))
            return;

        // Offset position where the sound is played
        var randomOffset = new Vector2(
            _random.NextFloat(-ent.Comp.MaxSoundDistance, ent.Comp.MaxSoundDistance),
            _random.NextFloat(-ent.Comp.MaxSoundDistance, ent.Comp.MaxSoundDistance)
        );

        var newCoords = Transform(ent).Coordinates.Offset(randomOffset);

        // Play the sound
        ent.Comp.Stream = _audio.PlayStatic(ent.Comp.Sounds, ent, newCoords)?.Entity;
    }

    /// <summary>
    /// Set a randomly generated time for the next incident to occur. Note this assumes that the component's
    /// <see cref="ParacusiaComponent.NextIncidentTime" /> is close to the current time.
    /// </summary>
    private void SetNewIncidentTime(Entity<ParacusiaComponent> ent)
    {
        ent.Comp.NextIncidentTime += _random.Next(ent.Comp.MinTimeBetweenIncidents, ent.Comp.MaxTimeBetweenIncidents);
    }
}
