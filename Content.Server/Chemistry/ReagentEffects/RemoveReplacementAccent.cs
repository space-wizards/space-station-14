using Content.Server.Speech.Components;

using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
/// Removes the ReplacementAccentComponent on the target, if any.
/// </summary>
public sealed class RemoveReplacementAccent : ReagentEffect
{
    /// <summary>
    /// Whether the chemical also removes the monkey accent, as it's similar to the replacement accents.
    /// </summary>
    [DataField("removesMonkeyAccent")]
    public bool RemovesMonkeyAccent { get; set; } = true;

    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        // This piece of code makes things able to speak "normally". One thing of note is that monkeys have a unique accent and won't be affected by this.
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);

        if (RemovesMonkeyAccent == true)
            // Monke talk if chemical is configured appropriately (on by default). This makes cognizine a cure to AMIV's long term damage funnily enough, do with this information what you will.
            entityManager.RemoveComponent<MonkeyAccentComponent>(uid);
    }
}
