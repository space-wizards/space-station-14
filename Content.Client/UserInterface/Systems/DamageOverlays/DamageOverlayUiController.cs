using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using System.Linq; // Offbrand
using Content.Shared._Offbrand.Wounds; // Offbrand

namespace Content.Client.UserInterface.Systems.DamageOverlays;

[UsedImplicitly]
public sealed class DamageOverlayUiController : UIController
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [UISystemDependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [UISystemDependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [UISystemDependency] private readonly DamageableSystem _damageable = default!;
    [UISystemDependency] private readonly PerfusionSystem _perfusion = default!; // Offbrand
    [UISystemDependency] private readonly PainSystem _pain = default!; // Offbrand

    private Overlays.DamageOverlay _overlay = default!;

    public override void Initialize()
    {
        _overlay = new Overlays.DamageOverlay();
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MobThresholdChecked>(OnThresholdCheck);
        SubscribeLocalEvent<PotentiallyUpdateDamageOverlayEvent>(OnPotentiallyUpdateDamageOverlay); // Offbrand
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        ClearOverlay();
        if (!EntityManager.TryGetComponent<MobStateComponent>(args.Entity, out var mobState))
            return;
        if (mobState.CurrentState != MobState.Dead)
            UpdateOverlays(args.Entity, mobState);
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
        ClearOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateOverlays(args.Target, args.Component);
    }

    private void OnThresholdCheck(ref MobThresholdChecked args)
    {

        if (args.Target != _playerManager.LocalEntity)
            return;
        UpdateOverlays(args.Target, args.MobState, args.Damageable, args.Threshold);
    }

    private void ClearOverlay()
    {
        _overlay.DeadLevel = 0f;
        _overlay.CritLevel = 0f;
        _overlay.PainLevel = 0f;
        _overlay.OxygenLevel = 0f;
        _overlay.AlwaysRenderAll = false; // Offbrand
    }

    //TODO: Jezi: adjust oxygen and hp overlays to use appropriate systems once bodysim is implemented
    private void UpdateOverlays(EntityUid entity, MobStateComponent? mobState, DamageableComponent? damageable = null, MobThresholdsComponent? thresholds = null, InjurableComponent? injurable = null)
    {
// Begin Offbrand Changes
        TryUpdateSimpleOverlays(entity, mobState, damageable, thresholds, injurable);
        TryUpdateWoundableOverlays(entity);
    }

    private void OnPotentiallyUpdateDamageOverlay(ref PotentiallyUpdateDamageOverlayEvent args)
    {
        if (args.Target != _playerManager.LocalEntity)
            return;

        UpdateOverlays(args.Target, null);
    }

    private void TryUpdateWoundableOverlays(EntityUid entity)
    {
        if (!EntityManager.TryGetComponent<PainComponent>(entity, out var pain) ||
            !EntityManager.TryGetComponent<ShockThresholdsComponent>(entity, out var shockThresholds) ||
            !EntityManager.TryGetComponent<BrainDamageThresholdsComponent>(entity, out var brainThresholds) ||
            !EntityManager.TryGetComponent<PerfusionComponent>(entity, out var perfusion))
            return;

        _overlay.AlwaysRenderAll = true;
        var maxBrain = brainThresholds.DamageStateThresholds.Keys.Max();
        var maxShock = shockThresholds.Thresholds.Keys.Max();

        switch (brainThresholds.CurrentState)
        {
            case MobState.Alive or MobState.Critical:
            {
                _overlay.CritLevel = FixedPoint2.Clamp(brainThresholds.DisplayDamage / maxBrain, 0, 1).Float();
                _overlay.PainLevel = FixedPoint2.Clamp(_pain.GetShock((entity, pain)) / maxShock, 0, 1).Float();
                _overlay.OxygenLevel = FixedPoint2.Clamp(1 - _perfusion.Spo2((entity, perfusion)), 0, 1).Float();
                _overlay.DeadLevel = 0;
                break;
            }
            case MobState.Dead:
            {
                _overlay.CritLevel = 0;
                _overlay.PainLevel = 0;
                _overlay.OxygenLevel = 0;
                break;
            }
        }

    }

    private void TryUpdateSimpleOverlays(EntityUid entity, MobStateComponent? mobState, DamageableComponent? damageable = null, MobThresholdsComponent? thresholds = null, InjurableComponent? injurable = null)
    {
// End Offbrand Changes
        if (mobState == null && !EntityManager.TryGetComponent(entity, out mobState) ||
            thresholds == null && !EntityManager.TryGetComponent(entity, out thresholds) ||
            damageable == null && !EntityManager.TryGetComponent(entity, out  damageable) ||
            injurable == null && !EntityManager.TryGetComponent(entity, out injurable))
            return;

        if (!_mobThresholdSystem.TryGetIncapThreshold(entity, out var foundThreshold, thresholds))
            return; //this entity cannot die or crit!!

        if (!thresholds.ShowOverlays)
        {
            ClearOverlay();
            return; //this entity intentionally has no overlays
        }

        var damagePerGroup = _damageable.GetDamagePerGroup((entity, damageable));
        var critThreshold = foundThreshold.Value;
        _overlay.State = mobState.CurrentState;

        switch (mobState.CurrentState)
        {
            case MobState.Alive:
            {
                FixedPoint2 painLevel = 0;
                _overlay.PainLevel = 0;

                if (!_statusEffects.TryEffectsWithComp<PainNumbnessStatusEffectComponent>(entity, out _))
                {
                    foreach (var painDamageType in injurable.PainDamageGroups)
                    {

                        damagePerGroup.TryGetValue(painDamageType, out var painDamage);
                        painLevel += painDamage;
                    }
                    _overlay.PainLevel = FixedPoint2.Min(1f, painLevel / critThreshold).Float();

                    if (_overlay.PainLevel < 0.05f) // Don't show damage overlay if they're near enough to max.
                    {
                        _overlay.PainLevel = 0;
                    }
                }

                if (damagePerGroup.TryGetValue("Airloss", out var oxyDamage))
                {
                    _overlay.OxygenLevel = FixedPoint2.Min(1f, oxyDamage / critThreshold).Float();
                }

                _overlay.CritLevel = 0;
                _overlay.DeadLevel = 0;
                break;
            }
            case MobState.Critical:
            {
                if (!_mobThresholdSystem.TryGetDeadPercentage(entity,
                        FixedPoint2.Max(0.0, _damageable.GetTotalDamage((entity, damageable))), out var critLevel))
                    return;
                _overlay.CritLevel = critLevel.Value.Float();

                _overlay.PainLevel = 0;
                _overlay.DeadLevel = 0;
                break;
            }
            case MobState.Dead:
            {
                _overlay.PainLevel = 0;
                _overlay.CritLevel = 0;
                break;
            }
        }
    }
}
