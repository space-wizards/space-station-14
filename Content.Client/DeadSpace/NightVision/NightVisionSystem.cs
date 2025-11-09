using Robust.Client.Graphics;
using Robust.Client.Player;
using Content.Shared.GameTicking;
using Robust.Shared.Player;
using Content.Shared.DeadSpace.NightVision;
using Content.Client.DeadSpace.Components.NightVision;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Robust.Client.Audio;

namespace Content.Client.DeadSpace.NightVision;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] ILightManager _lightManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    private NightVisionOverlay _overlay = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnNightVisionInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnNightVisionShutdown);

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);
        SubscribeLocalEvent<NightVisionComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<NightVisionComponent, ToggleNightVisionActionEvent>(OnToggleNightVision);

        _overlay = new();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var query = EntityQueryEnumerator<NightVisionComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_overlay.SoundBeenPlayed()
                && component.IsNightVision
                && !EntityManager.EntityExists(component.SoundEntity)
                && _overlay.GetTransitionProgress() >= 1f)
            {
                component.SoundEntity = _audio.PlayLocal(component.ActivateSound, uid, uid)?.Entity;
                _overlay.SetSoundBeenPlayed(false);
            }
        }
    }

    private void OnToggleNightVision(EntityUid uid, NightVisionComponent component, ref ToggleNightVisionActionEvent args)
    {
        if (args.Handled || component.IsToggled)
            return;

        args.Handled = true;

        component.ClientLastToggleTick = _timing.CurTick.Value;
        component.IsToggled = true;

        ToggleNightVision(uid, component, !component.IsNightVision);
    }

    private void ToggleNightVision(EntityUid uid, NightVisionComponent component, bool active)
    {
        if (_player.LocalEntity != uid)
            return;

        component.IsNightVision = active;

        if (!component.IsNightVision)
            _overlay.SetSoundBeenPlayed(true);
    }

    private void OnHandleState(EntityUid uid, NightVisionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NightVisionComponentState state)
            return;

        component.Color = state.Color;
        component.ActivateSound = state.ActivateSound;
        component.ServerLastToggleTick = state.LastToggleTick;

        // Применяю серверное состояние, если оно свежее предсказанного клиентом
        if (component.ClientLastToggleTick > component.ServerLastToggleTick)
            return;

        component.IsToggled = false;

        if (component.IsNightVision == state.IsNightVision)
            return;

        ToggleNightVision(uid, component, state.IsNightVision);

        component.ClientLastToggleTick = component.ServerLastToggleTick;
    }

    private void OnPlayerAttached(EntityUid uid, NightVisionComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, NightVisionComponent component, LocalPlayerDetachedEvent args)
    {
        RemNightVosion();
    }

    private void OnNightVisionInit(EntityUid uid, NightVisionComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnNightVisionShutdown(EntityUid uid, NightVisionComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            RemNightVosion();
    }

    private void RemNightVosion()
    {
        _overlay.Reset();
        _overlayMan.RemoveOverlay(_overlay);
        _lightManager.DrawLighting = true;
    }

    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _lightManager.DrawLighting = true;
    }
}
