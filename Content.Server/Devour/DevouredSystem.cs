using System.Linq;
using Content.Server.Devour.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Devour;

public sealed class DevouredSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevouredComponent, OnDevouredEvent>(OnDevoured);
        SubscribeLocalEvent<DevouredComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<DevouredComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    ///     Pacifies the target and gives them a passive damage effect the moment they are devoured.
    /// </summary>
    private void OnDevoured(EntityUid uid, DevouredComponent component, OnDevouredEvent args)
    {
        component.Devourer = args.Devourer;

        //If the target already had a passive damage component it will be stored so it can be returned
        //to it's original value later.
        if (EnsureComp<PassiveDamageComponent>(uid, out var passiveDamage))
            component.OriginalAllowedMobStates = passiveDamage.AllowedStates;

        //Stores the stomach damage value if it exists.
        if (TryComp<DevourerComponent>(args.Devourer, out var devourer))
        {
            if (devourer.StomachDamage != null)
                component.StomachDamage = devourer.StomachDamage;

            //Sets the MobStates in which the devoured entity is damaged.
            passiveDamage.AllowedStates = devourer.DigestibleStates;
        }

        //Sets the damage multiplier based on MobState.
        if (TryComp<MobStateComponent>(uid, out var mobState))
        {
            SetStomachDamage(mobState.CurrentState, component, passiveDamage);
        }

        //Sets the max damage to the damage needed for the mob to be considered dead +
        //the damage cap specified in the component. This can be used to make reviving take more work if
        //the target has been devoured for a long time.
        if (TryComp<MobThresholdsComponent>(uid, out var mobThresholds))
        {
            for (var mobStates = 0; mobStates < mobThresholds.Thresholds.Count; mobStates++)
            {
                var mobStateValue = mobThresholds.Thresholds.ElementAt(mobStates);
                if (mobStateValue.Value == MobState.Dead)
                {
                    passiveDamage.DamageCap = mobStateValue.Key + component.DamageCap;
                    break;
                }
            }
        }

        //Pacifies entities in the stomach and stores if the target was already pacified.
        //This is to make sure the pacified component won't be removed if the entity
        //already had it before being devoured.
        if (EnsureComp<PacifiedComponent>(uid, out var pacified))
            component.OriginallyPacified = true;
    }

    /// <summary>
    ///     Removes the effects that were added when the entity was devoured.
    /// </summary>
    private void OnRemove(EntityUid uid, DevouredComponent component, ComponentRemove args)
    {
        //Remove the component if the target originally didn't have it.
        if (component.OriginalAllowedMobStates == null)
        {
            RemComp<PassiveDamageComponent>(uid);
        }
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

        //Remove the pacified component if the target originally didn't have it.
        if (!component.OriginallyPacified)
            RemComp<PacifiedComponent>(uid);
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
