using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.MobState;

public sealed partial class MobStateSystem : SharedMobStateSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private Overlays.DamageOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new Overlays.DamageOverlay();
        _overlayManager.AddOverlay(_overlay);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetach);
        SubscribeLocalEvent<MobStateComponent, ComponentHandleState>(OnMobHandleState);
        SubscribeLocalEvent<MobStateComponent, AttackAttemptEvent>(OnAttack);
    }

    private void OnAttack(EntityUid uid, MobStateComponent component, AttackAttemptEvent args)
    {
        if (IsIncapacitated(uid, component))
            args.Cancel();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
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
        if (TryComp<MobStateComponent>(ev.Entity, out var mobState) && TryComp<DamageableComponent>(ev.Entity, out var damageable))
        {
            _overlay.DeadLevel = 0f;
            SetLevel(mobState, damageable.TotalDamage);
        }
        else
        {
            ClearOverlay();
        }
    }

    private void OnPlayerDetach(PlayerDetachedEvent ev)
    {
        ClearOverlay();
    }

    private void ClearOverlay()
    {
        _overlay.State = DamageState.Alive;
        _overlay.BruteLevel = 0f;
        _overlay.OxygenLevel = 0f;
        _overlay.CritLevel = 0f;
    }

    protected override void UpdateState(MobStateComponent component, DamageState? state, FixedPoint2 threshold)
    {
        base.UpdateState(component, state, threshold);
        SetLevel(component, threshold);
    }

    private void SetLevel(MobStateComponent stateComponent, FixedPoint2 threshold)
    {
        var uid = stateComponent.Owner;

        if (_playerManager.LocalPlayer?.ControlledEntity != uid) return;

        ClearOverlay();

        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        switch (stateComponent.CurrentState)
        {
            case DamageState.Dead:
                _overlay.State = DamageState.Dead;
                return;
        }

        var bruteLevel = 0f;
        var oxyLevel = 0f;
        var critLevel = 0f;

        if (TryGetEarliestIncapacitatedState(stateComponent, threshold, out _, out var earliestThreshold) && damageable.TotalDamage != 0)
        {
            if (damageable.DamagePerGroup.TryGetValue("Brute", out var bruteDamage))
            {
                bruteLevel = MathF.Min(1f, (bruteDamage / earliestThreshold).Float());
            }

            if (damageable.Damage.DamageDict.TryGetValue("Asphyxiation", out var oxyDamage))
            {
                oxyLevel = MathF.Min(1f, (oxyDamage / earliestThreshold).Float());
            }

            if (threshold >= earliestThreshold && TryGetEarliestDeadState(stateComponent, threshold, out _, out var earliestDeadHold))
            {
                critLevel = (float) Math.Clamp((damageable.TotalDamage - earliestThreshold).Double() / (earliestDeadHold - earliestThreshold).Double(), 0.1, 1);
            }
        }

        // Don't show damage overlay if they're near enough to max.

        if (bruteLevel < 0.05f)
        {
            bruteLevel = 0f;
        }

        _overlay.State = critLevel > 0f ? DamageState.Critical : DamageState.Alive;
        _overlay.BruteLevel = bruteLevel;
        _overlay.OxygenLevel = oxyLevel;
        _overlay.CritLevel = critLevel;
    }
}
