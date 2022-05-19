using Content.Shared.Light.Component;
using Content.Shared.Light.Systems;
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
    }

    private void OnHandleState(EntityUid uid, NightVisionComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NightVisionComponentState state) return;

        component.IsEnabled = state.IsEnabled;
        UpdateNightVision(uid, component);
    }

    private void UpdateNightVision(EntityUid uid, NightVisionComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var localPlayerPawn = _playerManager.LocalPlayer?.ControlledEntity;
        if (localPlayerPawn == null || localPlayerPawn != uid)
            return;

        _light.DrawShadows = !component.IsEnabled;
    }
}
