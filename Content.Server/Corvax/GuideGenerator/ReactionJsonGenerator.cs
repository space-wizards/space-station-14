using Content.Server.Corvax.GuideGenerator;
using Content.Shared.Chemistry.Reaction;
using Robust.Shared.Prototypes;

namespace Content.Server.GuideGenerator;
public sealed partial class ReactionJsonGenerator
{
    [ValidatePrototypeId<MixingCategoryPrototype>]
    private const string DefaultMixingCategory = "DummyMix";

    private static void AddMixingCategories(Dictionary<String, ReactionEntry> reactions, IPrototypeManager prototype)
    {
        foreach (var reaction in reactions)
        {
            var reactionPrototype = prototype.Index<ReactionPrototype>(reaction.Key);
            var mixingCategories = new List<MixingCategoryPrototype>();
            if (reactionPrototype.MixingCategories != null)
            {
                foreach (var category in reactionPrototype.MixingCategories)
                {
                    mixingCategories.Add(prototype.Index(category));
                }
            }
            else
            {
                mixingCategories.Add(prototype.Index<MixingCategoryPrototype>(DefaultMixingCategory));
            }

            foreach (var mixingCategory in mixingCategories)
            {
                reactions[reaction.Key].MixingCategories.Add(new MixingCategoryEntry(mixingCategory));
            }
        }
    }
}
