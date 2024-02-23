using Content.Shared.Damage.Components;
using Content.Shared.Devour.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Devour;

public sealed class DevouredSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevouredComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<DevouredComponent, DevouredEvent>(OnDevoured);
        SubscribeLocalEvent<DevouredComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<DevouredComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    ///     Prevents attacking while devoured.
    /// </summary>
    private void OnAttackAttempt(EntityUid uid, DevouredComponent component, AttackAttemptEvent args)
    {
        args.Cancel();
    }

    /// <summary>
    ///     Gives the target a passive damage effect the moment they are devoured.
    /// </summary>
    private void OnDevoured(Entity<DevouredComponent> entity, ref DevouredEvent args)
    {
        //If the target already had a passive damage component it will be stored so it can be returned
        //to it's original value later.
        if (EnsureComp<PassiveDamageComponent>(entity, out var passiveDamage))
            entity.Comp.OriginalAllowedMobStates = passiveDamage.AllowedStates;

        //Stores the stomach damage value if it exists.
        if (TryComp<DevourerComponent>(args.Devourer, out var devourer))
        {
            entity.Comp.StomachDamage = devourer.StomachDamage;

            //Sets the MobStates in which the devoured entity is damaged.
            passiveDamage.AllowedStates = devourer.DigestibleStates;
        }

        //Sets the damage multiplier based on MobState.
        if (TryComp<MobStateComponent>(entity, out var mobState))
            SetStomachDamage(mobState.CurrentState, entity.Comp, passiveDamage);

        //Sets the max damage to the damage needed for the mob to be considered dead +
        //the damage cap specified in the component. This can be used to make reviving take more work if
        //the target has been devoured for a long time.
        if (TryComp<MobThresholdsComponent>(entity, out var mobThresholds))
        {
            foreach (var threshold in mobThresholds.Thresholds)
            {
                if (threshold.Value == MobState.Dead)
                    passiveDamage.DamageCap = threshold.Key + entity.Comp.DamageCap;
            }
        }
    }

    /// <summary>
    ///     Removes the effects that were added when the entity was devoured.
    /// </summary>
    private void OnRemove(EntityUid uid, DevouredComponent component, ComponentRemove args)
    {
        //Remove the component if the target originally didn't have it.
        if (component.OriginalAllowedMobStates == null)
            RemComp<PassiveDamageComponent>(uid);
        else
        {
            //Return the passive damage component back to what it was before the entity got devoured.
            if (TryComp<PassiveDamageComponent>(uid, out var passiveDamage) && component.StomachDamage != null)
            {
                passiveDamage.Damage -= component.StomachDamage * component.CurrentModifier;
                passiveDamage.AllowedStates = component.OriginalAllowedMobStates;
                passiveDamage.Damage.TrimZeros();
            }
        }
    }

    /// <summary>
    ///     Changes the stomach damage based on the MobState of the devoured entity.
    ///     This allows for slower damage in crit/dead while not having someone just waiting for their death
    ///     while alive.
    /// </summary>
    private void OnMobStateChanged(EntityUid uid, DevouredComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<PassiveDamageComponent>(uid, out var passiveDamage) ||
            component.StomachDamage == null)
            return;

        SetStomachDamage(args.NewMobState, component, passiveDamage);
    }

    private void SetStomachDamage(MobState mobState, DevouredComponent component, PassiveDamageComponent passiveDamage)
    {
        if(component.StomachDamage == null)
            return;

        //Selects damage modifier based on MobState.
        var damageMultiplier = mobState switch
        {
            MobState.Alive => component.AliveMultiplier,
            MobState.Critical => component.CritMultiplier,
            MobState.Dead => component.DeadMultiplier,
            _ => new FixedPoint2()
        };

        //Remove the current damage modifier.
        passiveDamage.Damage -= component.StomachDamage * component.CurrentModifier;

        //Applies the new damage modifier.
        passiveDamage.Damage += component.StomachDamage * damageMultiplier;
        component.CurrentModifier = damageMultiplier;
    }
}
