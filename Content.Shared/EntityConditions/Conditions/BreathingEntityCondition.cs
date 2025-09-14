using Content.Shared.EntityEffects;

namespace Content.Shared.EntityConditions.Conditions;

public sealed class Breathing : EntityConditionBase<Breathing>
{
    /// <summary>
    ///     If true, the entity must not have trouble breathing to pass.
    /// </summary>
    [DataField]
    public bool IsBreathing = true;

    /*
    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-breathing",
            ("isBreathing", IsBreathing));
    }*/
}
