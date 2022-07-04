using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.MobState;

public sealed partial class MobStateSystem : SharedMobStateSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private Overlays.HealthOverlay _overlay = default!;

    public const int Levels = 7;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new Overlays.HealthOverlay();
        IoCManager.Resolve<IOverlayManager>().AddOverlay(_overlay);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetach);
        SubscribeLocalEvent<MobStateComponent, ComponentHandleState>(OnMobHandleState);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        IoCManager.Resolve<IOverlayManager>().RemoveOverlay(_overlay);
    }

    private void OnMobHandleState(EntityUid uid, MobStateComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobStateComponentState state) return;

        if (component.CurrentThreshold == state.CurrentThreshold)
           return;

        if (state.CurrentThreshold == null)
        {
            RemoveState(component);
        }
        else
        {
            UpdateState(component, state.CurrentThreshold.Value);
        }
    }

    private void OnPlayerAttach(PlayerAttachedEvent ev)
    {
        if (TryComp<MobStateComponent>(ev.Entity, out var mobState))
        {
            _overlay.Level = 0;
        }
    }

    private void OnPlayerDetach(PlayerDetachedEvent ev)
    {
        _overlay.Level = 0;
    }

    public void SetLevel(EntityUid uid, FixedPoint2 threshold)
    {
        if (_playerManager.LocalPlayer?.ControlledEntity != uid) return;

        short modifier = 0;
        _overlay.Level = modifier;

        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        if (!TryComp<MobStateComponent>(uid, out var stateComponent))
            return;

        if (stateComponent.TryGetEarliestIncapacitatedState(threshold, out _, out var earliestThreshold) && damageable.TotalDamage != 0)
        {
            modifier = (short)(damageable.TotalDamage / (earliestThreshold / 5) + 1);
        }

        _overlay.Level = modifier;
    }
}
