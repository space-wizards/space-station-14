using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SpillableSystem _spillableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    // TODO here
    // Update over time. Modify bloodloss damage in accordance with (amount of blood / max blood level), and reduce bleeding over time
    // Sub to damage changed event and modify bloodloss if incurring large hits of slashing/piercing

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var bloodstream in EntityManager.EntityQuery<BloodstreamComponent>())
        {
            bloodstream.AccumulatedFrametime += frameTime;

            if (bloodstream.AccumulatedFrametime < bloodstream.UpdateInterval)
                continue;

            bloodstream.AccumulatedFrametime -= bloodstream.UpdateInterval;

            var uid = bloodstream.Owner;
            if (TryComp<MobStateComponent>(uid, out var state) && state.IsDead())
                continue;

            // First, let's refresh their blood if possible.
            if (bloodstream.BloodSolution.CurrentVolume < bloodstream.BloodSolution.MaxVolume)
                TryModifyBloodLevel(uid, bloodstream.BloodRefreshAmount, false, bloodstream);

            // Next, let's remove some blood from them according to their bleed level.
            // as well as stop their bleeding to a certain extent.
            if (bloodstream.BleedAmount > 0)
            {
                TryModifyBloodLevel(uid, (-bloodstream.BleedAmount) / 10, true, bloodstream);
                TryModifyBleedAmount(uid, -bloodstream.BleedReductionAmount, bloodstream);
            }

            // Next, we'll deal some bloodloss damage if their blood level is below a threshold.
            var bloodPercentage = GetBloodLevelPercentage(uid, bloodstream);
            if (bloodPercentage < bloodstream.BloodlossThreshold)
            {
                // TODO use a better method for determining this.
                var amt = bloodstream.BloodlossDamage / bloodPercentage;

                _damageableSystem.TryChangeDamage(uid, amt, true, false);
            }
        }
    }

    private void OnComponentInit(EntityUid uid, BloodstreamComponent component, ComponentInit args)
    {
        component.ChemicalSolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultChemicalsSolutionName);
        component.BloodSolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultBloodSolutionName);

        component.ChemicalSolution.MaxVolume = component.ChemicalMaxVolume;
        component.BloodSolution.MaxVolume = component.BloodMaxVolume;

        // Fill blood solution with BLOOD
        _solutionContainerSystem.TryAddReagent(uid, component.BloodSolution, component.BloodReagent,
            component.BloodMaxVolume, out _);
    }

    private void OnDamageChanged(EntityUid uid, BloodstreamComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta is null)
            return;

        // TODO probably cache this or something. humans get hurt a lot
        if (!_prototypeManager.TryIndex<DamageModifierSetPrototype>(component.DamageBleedModifiers, out var modifiers))
            return;

        var bloodloss = DamageSpecifier.ApplyModifierSet(args.DamageDelta, modifiers).Total.Float();
        TryModifyBleedAmount(uid, bloodloss, component);
    }

    /// <summary>
    ///     Attempt to transfer provided solution to internal solution.
    /// </summary>
    public bool TryAddToChemicals(EntityUid uid, Solution solution, BloodstreamComponent? component=null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return _solutionContainerSystem.TryAddSolution(uid, component.ChemicalSolution, solution);
    }

    public float GetBloodLevelPercentage(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0.0f;

        return (component.BloodSolution.CurrentVolume / component.BloodSolution.MaxVolume).Float();
    }

    /// <summary>
    ///     Attempts to modify the blood level of this entity directly.
    /// </summary>
    public bool TryModifyBloodLevel(EntityUid uid, FixedPoint2 amount, bool makePuddle=false, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (amount >= 0)
            return _solutionContainerSystem.TryAddReagent(uid, component.BloodSolution, component.BloodReagent, amount, out _);

        // So we're removing blood, eh?
        if (makePuddle)
        {
            var puddleSolution = component.BloodSolution.SplitSolution(-amount);
            _spillableSystem.SpillAt(uid, puddleSolution, "PuddleSmear", false);
            return true;
        }

        return _solutionContainerSystem.TryRemoveReagent(uid, component.BloodSolution, component.BloodReagent, amount);
    }

    /// <summary>
    ///     Tries to make an entity bleed more or less
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public bool TryModifyBleedAmount(EntityUid uid, float amount, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        component.BleedAmount += amount;
        Math.Clamp(component.BleedAmount, 0, 100);

        return true;
    }
}
