using Content.Client.Items;
using Content.Client.Radiation.Components;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GeigerComponent, ItemStatusCollectMessage>(OnGetStatusMessage);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GeigerComponentState state)
            return;

        UpdateGeigerSound(uid, state, component);

        component.CurrentRadiation = state.CurrentRadiation;
        component.DangerLevel = state.DangerLevel;
        component.IsEnabled = state.IsEnabled;
        component.User = state.User;
        component.UiUpdateNeeded = true;
    }

    private void OnGetStatusMessage(EntityUid uid, GeigerComponent component, ItemStatusCollectMessage args)
    {
        if (!component.ShowControl)
            return;

        args.Controls.Add(new GeigerItemControl(component));
    }

    private void UpdateGeigerSound(EntityUid uid, GeigerComponentState newState, GeigerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // check if we even need to update sound
        if (newState.IsEnabled == component.IsEnabled &&
            newState.User == component.User &&
            newState.DangerLevel == component.DangerLevel)
        {
            return;
        }

        component.Stream?.Stop();

        if (!newState.IsEnabled || newState.User == null)
            return;
        if (!component.Sounds.TryGetValue(newState.DangerLevel, out var sounds))
            return;

        // check that that local player controls entity that is holding geiger counter
        if (_playerManager.LocalPlayer == null)
            return;
        var attachedEnt = _playerManager.LocalPlayer.Session.AttachedEntity;
        if (attachedEnt != newState.User)
            return;

        var sound = _audio.GetSound(sounds);
        var param = sounds.Params.WithLoop(true).WithVolume(-4f)
            .WithPlayOffset(_random.NextFloat(0.0f, 1f));
        component.Stream = _audio.Play(sound, Filter.Entities(newState.User.Value), uid, false, param);
    }
}
