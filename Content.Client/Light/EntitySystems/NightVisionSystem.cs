using Content.Shared.Light.Component;
using Content.Shared.Light.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Light.EntitySystems;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILightManager _light = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NightVisionComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnRemove);
    }

    private void OnHandleState(EntityUid uid, NightVisionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NightVisionComponentState state) return;

        component.IsEnabled = state.IsEnabled;
        UpdateNightVision(uid, component);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!TryComp(ev.Entity, out NightVisionComponent? component))
            return;

        _light.Enabled = !component.IsEnabled;
    }

    private void OnPlayerDetached(EntityUid uid, NightVisionComponent component, PlayerDetachedEvent args)
    {
        _light.Enabled = true;
    }

    private void OnRemove(EntityUid uid, NightVisionComponent component, ComponentRemove args)
    {
        _light.Enabled = true;
    }

    private void UpdateNightVision(EntityUid uid, NightVisionComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var localPlayerPawn = _playerManager.LocalPlayer?.ControlledEntity;
        if (localPlayerPawn == null || localPlayerPawn != uid)
            return;

        _light.Enabled = !component.IsEnabled;
    }
}
