using Content.Client.Items;
using Content.Client.Radiation.Components;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GeigerComponent, ItemStatusCollectMessage>(OnGetStatusMessage);
    }

    private void UpdateGeigerSound(EntityUid uid, GeigerDangerLevel level, GeigerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Stream?.Stop();
        if (!component.Sounds.TryGetValue(level, out var sounds))
            return;

        var sound = _audio.GetSound(sounds);
        var param = sounds.Params.WithLoop(true).WithVolume(-4f)
            .WithPlayOffset(_random.NextFloat(0.0f, 100.0f));
        component.Stream = _audio.Play(sound, Filter.Local(), uid, param);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GeigerComponentState state)
            return;

        var curLevel = component.DangerLevel;
        var newLevel = state.DangerLevel;
        if (curLevel != newLevel)
        {
            UpdateGeigerSound(uid, newLevel, component);
        }

        component.CurrentRadiation = state.CurrentRadiation;
        component.DangerLevel = state.DangerLevel;
        component.UiUpdateNeeded = true;
    }

    private void OnGetStatusMessage(EntityUid uid, GeigerComponent component, ItemStatusCollectMessage args)
    {
        if (!component.ShowControl)
            return;

        args.Controls.Add(new GeigerItemControl(component));
    }
}
