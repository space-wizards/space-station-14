using Content.Client.Alerts;
using Content.Client.Gameplay;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.DamageOverlays;

[UsedImplicitly]
public sealed class DamageOverlayUiController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [UISystemDependency] private readonly ClientAlertsSystem _alertsSystem = default!;
    [UISystemDependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    private Overlays.DamageOverlay _overlay = default!;

    public override void Initialize()
    {
        _overlay = new Overlays.DamageOverlay();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DamageChangedEvent>(OnDamageReceived);
    }

    public void OnStateEntered(GameplayState state)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    public void OnStateExited(GameplayState state)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttach(PlayerAttachedEvent args)
    {
        if (EntityManager.TryGetComponent<MobStateComponent>(args.Entity, out var mobState)
            && EntityManager.TryGetComponent<MobThresholdsComponent>(args.Entity, out var thresholds)
            && EntityManager.TryGetComponent<DamageableComponent>(args.Entity, out var damageable)
           )
        {
            OnStateUpdate(args.Entity, mobState.CurrentState, mobState, thresholds, damageable);
        }
        else
        {
            ClearOverlay();
        }
    }

    private void OnDamageReceived(DamageChangedEvent args)
    {
        var entity = args.Damageable.Owner;
        if (entity != _playerManager.LocalPlayer?.ControlledEntity || !EntityManager.TryGetComponent<MobStateComponent>(entity, out var mobStateComp))
            return;
        OnStateUpdate(entity, mobStateComp.CurrentState, mobStateComp, null, args.Damageable);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        ClearOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Entity != _playerManager.LocalPlayer?.ControlledEntity)
            return;
        OnStateUpdate(args.Entity, args.NewMobState, args.Component, null, null);
    }

    private void OnStateUpdate(EntityUid entity, MobState newMobState,MobStateComponent? mobState = null,
        MobThresholdsComponent? thresholds = null, DamageableComponent? damageable = null)
    {
        if (mobState == null && !EntityManager.TryGetComponent(entity, out mobState))
            return;
        if (thresholds == null && !EntityManager.TryGetComponent(entity, out thresholds))
            return;
        if (damageable == null && !EntityManager.TryGetComponent(entity, out damageable))
            return;
        switch (newMobState)
        {
            case MobState.Alive:
            {
                UpdateOverlay(entity, mobState, thresholds, damageable);
                break;
            }
            case MobState.Critical:
            {
                //_overlay.CritLevel = 1.0f;
                break;
            }
            case MobState.Dead:
            {
                //_overlay.DeadLevel = 1.0f;
                break;
            }
            case MobState.Invalid:
            default:
                return;
        }
        _overlay.State = mobState.CurrentState;
    }

    private void ClearOverlay()
    {
        _overlay.State = MobState.Alive;
        _overlay.BruteLevel = 0f;
        _overlay.OxygenLevel = 0f;
        _overlay.CritLevel = 0f;
    }
    //TODO: Jezi: adjust oxygen and hp overlays to use appropriate systems once bodysim is implemented
    private void UpdateOverlay(EntityUid entity, MobStateComponent mobState,
        MobThresholdsComponent thresholds, DamageableComponent damageable)
    {
        if (!_mobThresholdSystem.TryGetIncapThreshold(entity, out var foundThreshold, thresholds))
            return; //this entity cannot die or crit!!
        var threshold = foundThreshold.Value;

        if (damageable.TotalDamage == 0)
        {
            _overlay.BruteLevel = 0f;
            _overlay.OxygenLevel = 0f;
            _overlay.CritLevel = 0;
            return;
        }

        if (damageable.DamagePerGroup.TryGetValue("Brute", out var bruteDamage))
        {
            _overlay.BruteLevel = MathF.Min(1f,(bruteDamage/threshold).Float());
        }

        if (damageable.DamagePerGroup.TryGetValue("Asphyxiation", out var oxyDamage))
        {
            _overlay.BruteLevel = MathF.Min(1f,(oxyDamage/threshold).Float());
        }

        if (_overlay.BruteLevel < 0.05f) // Don't show damage overlay if they're near enough to max.
        {
            _overlay.BruteLevel = 0;
        }
        _overlay.State = mobState.CurrentState;

        if (!_mobThresholdSystem.TryGetIncapPercentage(entity, damageable.TotalDamage, out var critPercentage,
                thresholds))
        {
            _overlay.CritLevel = 0;
            return;
        }
        _overlay.CritLevel = critPercentage.Value.Float();
        _overlay.State = mobState.CurrentState;
    }
}
