using Content.Client.Gameplay;
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

    [UISystemDependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    private Overlays.DamageOverlay _overlay = default!;

    public override void Initialize()
    {
        _overlay = new Overlays.DamageOverlay();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
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
            _overlay.DeadLevel = 0f;
            UpdateOverlay_internal(args.Entity,mobState, thresholds, damageable);
        }
        else
        {
            ClearOverlay();
        }
    }
    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        ClearOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Entity != _playerManager.LocalPlayer?.ControlledEntity)
            return;
        switch (args.NewMobState)
        {
            case MobState.Alive:
            {
                UpdateOverlay(args.Entity, args.Component);
                break;
            }
            case MobState.Critical:
            case MobState.Dead:
            {
                _overlay.CritLevel = 1.0f;
                break;
            }
            case MobState.Invalid:
            default:
                return;
        }
    }
    private void ClearOverlay()
    {
        _overlay.State = MobState.Alive;
        _overlay.BruteLevel = 0f;
        _overlay.OxygenLevel = 0f;
        _overlay.CritLevel = 0f;
    }
    //TODO: Jezi: adjust oxygen and hp overlays to use appropriate systems once bodysim is implemented
    private void UpdateOverlay_internal(EntityUid entity,MobStateComponent mobStateComp, MobThresholdsComponent thresholds, DamageableComponent damageable)
    {
        if (!_mobThresholdSystem.TryGetIncapThreshold(entity, out var foundThreshold, thresholds))
            return; //this entity cannot die or crit!!
        var threshold = foundThreshold.Value;

        if (damageable.TotalDamage == 0)
        {
            _overlay.BruteLevel = 0f;
            _overlay.OxygenLevel = 0f;
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
        _overlay.State = mobStateComp.CurrentState;

        if (!_mobThresholdSystem.TryGetIncapPercentage(entity, damageable.TotalDamage, out var critPercentage,
                thresholds))
        {
            _overlay.CritLevel = 0;
            return;
        }
        _overlay.CritLevel = critPercentage.Value.Float();
        _overlay.State = mobStateComp.CurrentState;
    }

    private void UpdateOverlay(EntityUid entity, MobStateComponent? mobState = null,
        MobThresholdsComponent? thresholds = null, DamageableComponent? damageable = null)
    {
        if ((mobState != null ||EntityManager.TryGetComponent(entity, out  mobState))
            && (thresholds != null ||EntityManager.TryGetComponent(entity, out  thresholds))
            && (damageable != null || EntityManager.TryGetComponent(entity, out  damageable)))
        {
            UpdateOverlay_internal(entity, mobState, thresholds, damageable);
        }
    }
}
