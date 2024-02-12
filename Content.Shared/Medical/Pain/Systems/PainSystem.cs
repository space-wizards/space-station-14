using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.Systems;


public sealed class PainSystem : EntitySystem
{

    //TODO: convert thresholds to use Health Condition entity prototypes (except for unconsciousness)

    public void CheckPainThresholds(Entity<NervousSystemComponent?> nervousSystem)
    {
        if (!Resolve(nervousSystem, ref nervousSystem.Comp))
            return;
        var pain = nervousSystem.Comp.Pain;
        if (pain > nervousSystem.Comp.UnConsciousThresholdPain
            && !nervousSystem.Comp.AppliedEffects.HasFlag(PainEffect.UnConscious))
        {
            nervousSystem.Comp.AppliedEffects |= PainEffect.UnConscious;
            //TODO: cause unconsious
        }
        if (pain > nervousSystem.Comp.ShockThresholdPain
            && !nervousSystem.Comp.AppliedEffects.HasFlag(PainEffect.Shock))
        {
            nervousSystem.Comp.AppliedEffects |= PainEffect.Shock;
            //TODO: cause shock
        }
        if (pain > nervousSystem.Comp.HeartAttackThreshold
            && !nervousSystem.Comp.AppliedEffects.HasFlag(PainEffect.HeartAttack))
        {
            nervousSystem.Comp.AppliedEffects |= PainEffect.HeartAttack;
            //TODO: cause heart attack
        }
        Dirty(nervousSystem, nervousSystem.Comp);
    }

}
