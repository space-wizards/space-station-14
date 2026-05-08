using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Shadowkin;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Shadowkin;

public sealed class ShadowkinLightDampeningSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly (float Offset, bool Enabled)[] FlickerSteps =
    {
        (0.00f, true),
        (0.05f, false),
        (0.75f, true),
        (0.81f, false),
        (1.31f, true),
        (1.39f, false),
        (1.74f, true),
        (1.84f, false),
        (2.06f, true),
        (2.18f, false),
        (2.36f, true),
        (2.52f, false),
        (2.66f, true),
        (2.88f, false),
        (2.98f, true),
        (3.30f, false),
        (3.38f, true),
        (3.82f, false),
        (3.88f, true),
        (4.45f, false),
        (4.55f, true),
    };

    private readonly HashSet<EntityUid> _nearby = new();
    private readonly List<EntityUid> _soundCandidates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinLightDampeningComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShadowkinLightDampeningComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadowkinLightDampeningComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime >= component.EndTime)
            {
                RestoreLights(component);
                RemCompDeferred<ShadowkinLightDampeningComponent>(uid);
                continue;
            }

            UpdateFlicker(component);
        }
    }

    private void OnMapInit(Entity<ShadowkinLightDampeningComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StartTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.FlickerStartDelay);
        ent.Comp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Duration);
        ent.Comp.Applied = true;
        ent.Comp.BlackoutApplied = false;
        ent.Comp.NextFlickerStep = 0;

        _nearby.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius, _nearby, LookupFlags.Uncontained);

        foreach (var uid in _nearby)
        {
            if (TryComp<PoweredLightComponent>(uid, out var powered))
            {
                if (!powered.On)
                    continue;

                ent.Comp.AffectedLights.Add(new ShadowkinLightState(uid, true, true));
                continue;
            }

            if (_pointLight.TryGetLight(uid, out var light) && light.Enabled)
                ent.Comp.AffectedLights.Add(new ShadowkinLightState(uid, true, false));
        }
    }

    private void OnShutdown(Entity<ShadowkinLightDampeningComponent> ent, ref ComponentShutdown args)
    {
        RestoreLights(ent.Comp);
    }

    private void RestoreLights(ShadowkinLightDampeningComponent component)
    {
        if (!component.Applied)
            return;

        SetAffectedLights(component, true);
        component.AffectedLights.Clear();
        component.Applied = false;
    }

    private void UpdateFlicker(ShadowkinLightDampeningComponent component)
    {
        if (!component.BlackoutApplied)
        {
            if (_timing.CurTime < component.StartTime)
                return;

            SetAffectedLights(component, false);
            component.BlackoutApplied = true;
        }

        var flickerStart = component.StartTime + TimeSpan.FromSeconds(component.BlackoutDuration);
        while (component.NextFlickerStep < FlickerSteps.Length)
        {
            var step = FlickerSteps[component.NextFlickerStep];
            if (_timing.CurTime < flickerStart + TimeSpan.FromSeconds(step.Offset))
                return;

            SetAffectedLights(component, step.Enabled);
            if (step.Enabled)
                PlayFlickerSounds(component);

            component.NextFlickerStep++;
        }
    }

    private void SetAffectedLights(ShadowkinLightDampeningComponent component, bool enabled)
    {
        foreach (var lightState in component.AffectedLights)
        {
            if (TerminatingOrDeleted(lightState.Entity))
                continue;

            var targetOn = enabled && lightState.WasEnabled;

            if (lightState.IsPowered &&
                TryComp<PoweredLightComponent>(lightState.Entity, out var powered))
            {
                _poweredLight.SetState(lightState.Entity, targetOn, powered);
                continue;
            }

            if (_pointLight.TryGetLight(lightState.Entity, out var light))
                _pointLight.SetEnabled(lightState.Entity, targetOn, light);
        }
    }

    private void PlayFlickerSounds(ShadowkinLightDampeningComponent component)
    {
        if (component.FlickerSound == null || component.MaxSoundsPerStep <= 0)
            return;

        _soundCandidates.Clear();
        foreach (var state in component.AffectedLights)
        {
            if (!state.WasEnabled || TerminatingOrDeleted(state.Entity))
                continue;

            _soundCandidates.Add(state.Entity);
        }

        if (_soundCandidates.Count == 0)
            return;

        var count = Math.Min(component.MaxSoundsPerStep, _soundCandidates.Count);
        for (var i = 0; i < count; i++)
        {
            var pickIdx = _random.Next(_soundCandidates.Count - i);
            var picked = _soundCandidates[pickIdx];
            var lastIdx = _soundCandidates.Count - 1 - i;
            _soundCandidates[pickIdx] = _soundCandidates[lastIdx];
            _soundCandidates[lastIdx] = picked;

            _audio.PlayPvs(component.FlickerSound, picked);
        }
    }
}
