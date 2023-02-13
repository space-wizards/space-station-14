using Content.Client.Items;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachSysMessage>(OnAttachedEntityChanged);
        SubscribeLocalEvent<GeigerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<GeigerComponent, ItemStatusCollectMessage>(OnGetStatusMessage);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GeigerComponentState state)
            return;

        UpdateGeigerSound(uid, state.IsEnabled, state.User, state.DangerLevel, false, component);

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

    private void OnAttachedEntityChanged(PlayerAttachSysMessage ev)
    {
        // need to go for each component known to client
        // and update their geiger sound
        foreach (var geiger in EntityQuery<GeigerComponent>())
        {
            ForceUpdateGeigerSound(geiger.Owner, geiger);
        }
    }

    private void ForceUpdateGeigerSound(EntityUid uid, GeigerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        UpdateGeigerSound(uid, component.IsEnabled, component.User, component.DangerLevel, true, component);
    }

    private void UpdateGeigerSound(EntityUid uid, bool isEnabled, EntityUid? user,
        GeigerDangerLevel dangerLevel, bool force = false, GeigerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // check if we even need to update sound
        if (!force && isEnabled == component.IsEnabled &&
            user == component.User && dangerLevel == component.DangerLevel)
        {
            return;
        }

        component.Stream?.Stop();

        if (!isEnabled || user == null)
            return;
        if (!component.Sounds.TryGetValue(dangerLevel, out var sounds))
            return;

        // check that that local player controls entity that is holding geiger counter
        if (_playerManager.LocalPlayer == null)
            return;
        var attachedEnt = _playerManager.LocalPlayer.Session.AttachedEntity;
        if (attachedEnt != user)
            return;

        var sound = _audio.GetSound(sounds);
        var param = sounds.Params.WithLoop(true).WithVolume(-4f);
        component.Stream = _audio.Play(sound, Filter.Local(), uid, false, param);
    }
}
